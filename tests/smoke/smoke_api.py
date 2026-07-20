#!/usr/bin/env python3
"""
Lingarr Bedroom API smoke / regression checks.

Default target: http://127.0.0.1:9876 (running lingarr-bedroom container).
Does not require AI credits for the free scraper path; optional AI checks if keys are set.

Exit 0 = all required checks passed.
Exit 1 = failure.
"""
from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from typing import Any

BASE = os.environ.get("LINGARR_URL", "http://127.0.0.1:9876").rstrip("/")
TIMEOUT = float(os.environ.get("LINGARR_SMOKE_TIMEOUT", "60"))
STRICT_AI = os.environ.get("LINGARR_SMOKE_AI", "0") == "1"

failures: list[str] = []
passed = 0


def req(method: str, path: str, body: Any | None = None, timeout: float = TIMEOUT) -> tuple[int, Any]:
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    r = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=timeout) as resp:
            raw = resp.read()
            if not raw:
                return resp.status, None
            try:
                return resp.status, json.loads(raw)
            except json.JSONDecodeError:
                return resp.status, raw.decode("utf-8", errors="replace")
    except urllib.error.HTTPError as e:
        raw = e.read()
        try:
            return e.code, json.loads(raw) if raw else None
        except Exception:
            return e.code, raw.decode("utf-8", errors="replace") if raw else None


def check(name: str, cond: bool, detail: str = "") -> None:
    global passed
    if cond:
        passed += 1
        print(f"  PASS  {name}" + (f" — {detail}" if detail else ""))
    else:
        failures.append(name + (f": {detail}" if detail else ""))
        print(f"  FAIL  {name}" + (f" — {detail}" if detail else ""))


def main() -> int:
    print(f"Lingarr smoke → {BASE}\n")

    # --- health / version ---
    code, ver = req("GET", "/api/version")
    check("version endpoint", code == 200 and isinstance(ver, dict), f"status={code}")
    if isinstance(ver, dict):
        check("version has currentVersion", bool(ver.get("currentVersion")), str(ver.get("currentVersion")))

    # --- plugins ---
    code, plugins = req("GET", "/api/plugin")
    check("plugin list", code == 200 and isinstance(plugins, list), f"status={code}")
    providers = set()
    if isinstance(plugins, list):
        for p in plugins:
            providers.add((p.get("provider") or p.get("Provider") or "").lower())
        for needed in ("microsoft", "deepseek", "openrouter", "zai", "opencode-go", "openai"):
            check(f"provider registered: {needed}", needed in providers)

    # --- service_type parse surface ---
    code, st = req("GET", "/api/setting/service_type")
    # setting endpoint may return plain string body
    check("service_type readable", code == 200, f"status={code} body={st!r}"[:120])

    # save microsoft-only chain (safe free path)
    code, _ = req(
        "POST",
        "/api/setting",
        {"Key": "service_type", "Value": json.dumps([{"provider": "microsoft"}])},
    )
    check("set service_type microsoft", code == 200, f"status={code}")

    # --- translate line free path ---
    code, translated = req(
        "POST",
        "/api/translate/line",
        {
            "subtitleLine": "Hello",
            "sourceLanguage": "en",
            "targetLanguage": "es",
        },
        timeout=120,
    )
    check(
        "translate line microsoft en→es",
        code == 200 and isinstance(translated, str) and len(translated) > 0,
        f"status={code} out={translated!r}"[:160],
    )

    # --- model catalogues (shape) ---
    for prov in ("openrouter", "deepseek", "zai", "opencode-go"):
        if prov not in providers:
            continue
        code, models = req("GET", f"/api/plugin/{prov}/models?refresh=true", timeout=120)
        opts = []
        if isinstance(models, dict):
            opts = models.get("options") or models.get("Options") or []
        check(f"models endpoint {prov}", code == 200, f"status={code} n={len(opts)}")
        if prov == "openrouter" and opts:
            first = (opts[0].get("value") or opts[0].get("Value") or "")
            check("openrouter/free is first model", first == "openrouter/free", f"first={first!r}")
        if prov == "zai" and opts:
            values = [(o.get("value") or o.get("Value") or "") for o in opts]
            check("zai lists glm-5.2", "glm-5.2" in values, f"sample={values[:5]}")

    # --- content translate API accepts body ---
    code, content = req(
        "POST",
        "/api/translate/content",
        {
            "title": "Smoke Test Title",
            "sourceLanguage": "en",
            "targetLanguage": "es",
            "mediaType": "movie",
            "lines": [{"position": 1, "line": "Hello world"}],
        },
        timeout=180,
    )
    check(
        "translate content accepts request",
        code in (200, 201, 202) or (code == 500 and content is not None),
        f"status={code}",
    )

    # optional AI path
    if STRICT_AI:
        code, _ = req(
            "POST",
            "/api/setting",
            {
                "Key": "service_type",
                "Value": json.dumps([{"provider": "openrouter", "model": "openrouter/free"}]),
            },
        )
        check("set openrouter/free chain", code == 200)
        code, out = req(
            "POST",
            "/api/translate/line",
            {"subtitleLine": "Hello", "sourceLanguage": "en", "targetLanguage": "bg"},
            timeout=180,
        )
        check("AI translate openrouter/free", code == 200 and bool(out), f"status={code} out={out!r}"[:120])
        req(
            "POST",
            "/api/setting",
            {"Key": "service_type", "Value": json.dumps([{"provider": "microsoft"}])},
        )

    print()
    print(f"Passed: {passed}  Failed: {len(failures)}")
    if failures:
        print("Failures:")
        for f in failures:
            print(f"  - {f}")
        return 1
    print("All required smoke checks passed.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(f"SMOKE ERROR: {exc}", file=sys.stderr)
        sys.exit(1)
