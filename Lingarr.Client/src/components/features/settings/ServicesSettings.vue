<template>
    <CardComponent title="Translation Services">
        <template #description>
            Configure the translation service for subtitle localization. Each row is self-contained:
            provider, model (when needed), and API key. Fallbacks run in order if earlier rows fail.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />

            <div class="space-y-2">
                <span class="font-semibold">Translation services</span>
                <ol class="space-y-3">
                    <li
                        v-for="(entry, index) in chain"
                        :key="entry.id"
                        class="border-accent/30 flex gap-3 rounded-md border p-3">
                        <!-- 1-based index badge (primary = 1) -->
                        <span
                            class="bg-accent/20 text-accent-content mt-2 flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-sm font-semibold tabular-nums"
                            :title="index === 0 ? 'Primary' : `Fallback ${index}`"
                            :aria-label="index === 0 ? 'Primary' : `Fallback ${index}`">
                            {{ index + 1 }}
                        </span>

                        <div class="flex min-w-0 flex-1 flex-col gap-2">
                            <!-- Row 1: provider -->
                            <SelectComponent
                                :selected="entry.provider"
                                :options="providerOptions"
                                placeholder="Select provider..."
                                @update:selected="(value: string) => setProvider(index, value)" />

                            <!-- Row 2: model (AI / multi-model providers) -->
                            <div
                                v-if="supportsModel(entry.provider)"
                                class="flex items-center gap-2">
                                <div class="min-w-0 flex-1">
                                    <SelectComponent
                                        :ref="(el) => setModelSelectRef(index, el)"
                                        :selected="entry.model ?? ''"
                                        :options="modelOptions[index] || []"
                                        :load-on-open="true"
                                        :sort-options="false"
                                        placeholder="Select model..."
                                        :no-options="modelError[index] || 'Loading models...'"
                                        @update:selected="(value: string) => setModel(index, value)"
                                        @fetch-options="() => loadModels(index, false)" />
                                </div>
                                <ButtonComponent
                                    variant="ghost"
                                    size="xs"
                                    title="Refresh models"
                                    @click="loadModels(index, true)">
                                    Refresh
                                </ButtonComponent>
                            </div>

                            <div
                                v-if="supportsInstructions(entry.provider)"
                                class="grid gap-2 rounded-md border border-accent/20 bg-primary/35 p-2 sm:grid-cols-2">
                                <label>
                                    <span class="mb-1 block text-xs font-semibold text-primary-content/65">
                                        System prompt
                                    </span>
                                    <select
                                        :value="entry.systemPromptProfileId ?? ''"
                                        class="w-full rounded-md border border-accent bg-secondary px-2 py-2 text-sm text-primary-content"
                                        @change="
                                            setPromptProfile(
                                                index,
                                                'system',
                                                ($event.target as HTMLSelectElement).value
                                            )
                                        ">
                                        <option value="">
                                            Default · {{ activeSystemProfileName }}
                                        </option>
                                        <option
                                            v-for="profile in systemProfiles"
                                            :key="profile.id"
                                            :value="profile.id">
                                            {{ profile.name }} · v{{ profile.currentVersionNumber ?? 'draft' }}
                                        </option>
                                    </select>
                                </label>
                                <label>
                                    <span class="mb-1 block text-xs font-semibold text-primary-content/65">
                                        Context prompt
                                    </span>
                                    <select
                                        :value="entry.contextPromptProfileId ?? ''"
                                        class="w-full rounded-md border border-accent bg-secondary px-2 py-2 text-sm text-primary-content"
                                        @change="
                                            setPromptProfile(
                                                index,
                                                'context',
                                                ($event.target as HTMLSelectElement).value
                                            )
                                        ">
                                        <option value="">
                                            Default · {{ activeContextProfileName }}
                                        </option>
                                        <option
                                            v-for="profile in contextProfiles"
                                            :key="profile.id"
                                            :value="profile.id">
                                            {{ profile.name }} · v{{ profile.currentVersionNumber ?? 'draft' }}
                                        </option>
                                    </select>
                                </label>
                                <router-link
                                    :to="{ name: 'translation-prompts-settings' }"
                                    class="text-xs text-accent underline sm:col-span-2">
                                    Manage prompt profiles
                                </router-link>
                            </div>

                            <!-- API key (per provider, only when needed) -->
                            <InputComponent
                                v-if="apiKeySettingKey(entry.provider)"
                                :id="`api-key-${index}-${entry.provider}`"
                                :model-value="apiKeyValue(entry.provider)"
                                :type="INPUT_TYPE.PASSWORD"
                                placeholder="API key"
                                @update:model-value="(v: string) => setApiKey(entry.provider, v)" />
                        </div>

                        <div class="flex shrink-0 flex-col items-center gap-0.5 pt-1">
                            <button
                                type="button"
                                class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                                :disabled="index === 0"
                                title="Move up"
                                aria-label="Move up"
                                @click="moveRow(index, -1)">
                                <CaretUpIcon class="h-4 w-4" />
                            </button>
                            <button
                                type="button"
                                class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                                :disabled="index === chain.length - 1"
                                title="Move down"
                                aria-label="Move down"
                                @click="moveRow(index, 1)">
                                <CaretDownIcon class="h-4 w-4" />
                            </button>
                            <button
                                v-if="index > 0"
                                type="button"
                                class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none"
                                title="Remove fallback"
                                aria-label="Remove fallback"
                                @click="removeRow(index)">
                                <TrashIcon class="h-4 w-4" />
                            </button>
                        </div>
                    </li>
                    <li>
                        <ButtonComponent variant="ghost" size="xs" @click="addRow">
                            <PlusIcon class="mr-1 h-3 w-3" />
                            Add fallback service
                        </ButtonComponent>
                    </li>
                </ol>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useSettingStore } from '@/store/setting'
import {
    ENCRYPTED_SETTINGS,
    IEncryptedSettings,
    INPUT_TYPE,
    PLUGIN_SETTING_TYPE,
    IPluginSummary,
    IPromptProfile,
    SETTINGS,
    SERVICE_TYPE,
    SelectComponentExpose
} from '@/ts'
import servicesApi from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import CaretUpIcon from '@/components/icons/CaretUpIcon.vue'
import CaretDownIcon from '@/components/icons/CaretDownIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import PlusIcon from '@/components/icons/PlusIcon.vue'

export type ChainEntry = {
    id: string
    provider: string
    model?: string | null
    systemPromptProfileId?: number | null
    contextPromptProfileId?: number | null
}

const MODEL_PROVIDERS = new Set([
    'openai',
    'anthropic',
    'gemini',
    'deepseek',
    'localai',
    'openrouter',
    'zai',
    'opencode-go',
    'qwen',
    'qwen-mt',
    'xai',
    'xai-oauth'
])

/** Providers that use an encrypted API key setting. */
const API_KEY_BY_PROVIDER: Record<string, keyof IEncryptedSettings> = {
    openai: ENCRYPTED_SETTINGS.OPENAI_API_KEY as keyof IEncryptedSettings,
    anthropic: ENCRYPTED_SETTINGS.ANTHROPIC_API_KEY as keyof IEncryptedSettings,
    gemini: ENCRYPTED_SETTINGS.GEMINI_API_KEY as keyof IEncryptedSettings,
    deepseek: ENCRYPTED_SETTINGS.DEEPSEEK_API_KEY as keyof IEncryptedSettings,
    openrouter: ENCRYPTED_SETTINGS.OPENROUTER_API_KEY as keyof IEncryptedSettings,
    zai: ENCRYPTED_SETTINGS.ZAI_API_KEY as keyof IEncryptedSettings,
    'opencode-go': ENCRYPTED_SETTINGS.OPENCODE_GO_API_KEY as keyof IEncryptedSettings,
    qwen: ENCRYPTED_SETTINGS.QWEN_API_KEY as keyof IEncryptedSettings,
    'qwen-mt': ENCRYPTED_SETTINGS.QWEN_API_KEY as keyof IEncryptedSettings,
    xai: ENCRYPTED_SETTINGS.XAI_API_KEY as keyof IEncryptedSettings,
    deepl: ENCRYPTED_SETTINGS.DEEPL_API_KEY as keyof IEncryptedSettings,
    libretranslate: ENCRYPTED_SETTINGS.LIBRETRANSLATE_API_KEY as keyof IEncryptedSettings,
    localai: ENCRYPTED_SETTINGS.LOCAL_AI_API_KEY as keyof IEncryptedSettings
}

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const providerOptions = ref<{ value: string; label: string }[]>([])
const chain = ref<ChainEntry[]>([
    { id: crypto.randomUUID(), provider: SERVICE_TYPE.LIBRETRANSLATE }
])
const promptProfiles = ref<IPromptProfile[]>([])
const instructionProviders = ref(new Set<string>())
const activeSystemProfileId = ref(0)
const activeContextProfileId = ref(0)
const modelOptions = reactive<Record<number, { value: string; label: string }[]>>({})
const modelError = reactive<Record<number, string | null>>({})
const modelSelectRefs = ref<Record<number, SelectComponentExpose | null>>({})

function setModelSelectRef(index: number, el: unknown) {
    if (el) {
        modelSelectRefs.value[index] = el as SelectComponentExpose
    } else {
        delete modelSelectRefs.value[index]
    }
}

function supportsModel(provider?: string) {
    return !!provider && MODEL_PROVIDERS.has(provider.toLowerCase())
}

function supportsInstructions(provider?: string) {
    return !!provider && instructionProviders.value.has(provider.toLowerCase())
}

const systemProfiles = computed(() =>
    promptProfiles.value.filter(
        (profile) => profile.type === 'system' && profile.currentPublishedVersionId
    )
)
const contextProfiles = computed(() =>
    promptProfiles.value.filter(
        (profile) => profile.type === 'context' && profile.currentPublishedVersionId
    )
)
const activeSystemProfileName = computed(
    () =>
        systemProfiles.value.find((profile) => profile.id === activeSystemProfileId.value)?.name ??
        'None'
)
const activeContextProfileName = computed(
    () =>
        contextProfiles.value.find((profile) => profile.id === activeContextProfileId.value)?.name ??
        'None'
)

function apiKeySettingKey(provider?: string): keyof IEncryptedSettings | null {
    if (!provider) return null
    return API_KEY_BY_PROVIDER[provider.toLowerCase()] ?? null
}

function apiKeyValue(provider: string): string {
    const key = apiKeySettingKey(provider)
    if (!key) return ''
    const stored = settingsStore.getEncryptedSetting(key)
    return typeof stored === 'string' ? stored : ''
}

function setApiKey(provider: string, value: string) {
    const key = apiKeySettingKey(provider)
    if (!key) return
    settingsStore.updateEncryptedSetting(key, value, true)
    saveNotification.value?.show()
}

function parseChain(raw: unknown): ChainEntry[] {
    try {
        const text = (raw as string) ?? '[]'
        if (!text.trim().startsWith('[')) {
            return [
                {
                    id: crypto.randomUUID(),
                    provider: text.trim() || SERVICE_TYPE.LIBRETRANSLATE
                }
            ]
        }
        const parsed = JSON.parse(text) as unknown[]
        if (!Array.isArray(parsed) || parsed.length === 0) {
            return [{ id: crypto.randomUUID(), provider: SERVICE_TYPE.LIBRETRANSLATE }]
        }
        return parsed.map((item) => {
            if (typeof item === 'string') return { id: crypto.randomUUID(), provider: item }
            const obj = item as {
                id?: string
                provider?: string
                service?: string
                model?: string
                systemPromptProfileId?: number
                contextPromptProfileId?: number
            }
            return {
                id: obj.id || crypto.randomUUID(),
                provider: obj.provider || obj.service || SERVICE_TYPE.LIBRETRANSLATE,
                model: obj.model || null,
                systemPromptProfileId: obj.systemPromptProfileId ?? null,
                contextPromptProfileId: obj.contextPromptProfileId ?? null
            }
        })
    } catch {
        return [{ id: crypto.randomUUID(), provider: SERVICE_TYPE.LIBRETRANSLATE }]
    }
}

async function save(next: ChainEntry[]) {
    chain.value = next
    const payload = next.map((entry) => ({
        id: entry.id,
        provider: entry.provider,
        ...(entry.model ? { model: entry.model } : {}),
        ...(entry.systemPromptProfileId
            ? { systemPromptProfileId: entry.systemPromptProfileId }
            : {}),
        ...(entry.contextPromptProfileId
            ? { contextPromptProfileId: entry.contextPromptProfileId }
            : {})
    }))
    await settingsStore.updateSetting(SETTINGS.SERVICE_TYPE, JSON.stringify(payload), true)
    for (const entry of next) {
        if (!entry.model || !supportsModel(entry.provider)) continue
        const key = modelSettingKey(entry.provider)
        if (key) {
            await settingsStore.updateSetting(key as any, entry.model, true)
        }
    }
    saveNotification.value?.show()
}

function modelSettingKey(provider: string): string | null {
    const map: Record<string, string> = {
        openai: SETTINGS.OPENAI_MODEL,
        anthropic: SETTINGS.ANTHROPIC_MODEL,
        gemini: SETTINGS.GEMINI_MODEL,
        deepseek: SETTINGS.DEEPSEEK_MODEL,
        localai: SETTINGS.LOCAL_AI_MODEL,
        openrouter: (SETTINGS as any).OPENROUTER_MODEL,
        zai: (SETTINGS as any).ZAI_MODEL,
        'opencode-go': (SETTINGS as any).OPENCODE_GO_MODEL,
        qwen: (SETTINGS as any).QWEN_MODEL,
        'qwen-mt': (SETTINGS as any).QWEN_MT_MODEL,
        xai: (SETTINGS as any).XAI_MODEL,
        'xai-oauth': (SETTINGS as any).XAI_OAUTH_MODEL
    }
    return map[provider.toLowerCase()] ?? null
}

function setProvider(index: number, value: string) {
    const next = chain.value.map((e, i) =>
        i === index
            ? {
                  ...e,
                  provider: value,
                  model: supportsModel(value) ? e.model : null,
                  systemPromptProfileId: supportsInstructions(value)
                      ? e.systemPromptProfileId
                      : null,
                  contextPromptProfileId: supportsInstructions(value)
                      ? e.contextPromptProfileId
                      : null
              }
            : e
    )
    save(next)
    loadModels(index, false)
}

function setPromptProfile(index: number, type: 'system' | 'context', value: string) {
    const profileId = value ? Number(value) : null
    const next = chain.value.map((entry, rowIndex) => {
        if (rowIndex !== index) return entry
        return type === 'system'
            ? { ...entry, systemPromptProfileId: profileId }
            : { ...entry, contextPromptProfileId: profileId }
    })
    save(next)
}

function setModel(index: number, value: string) {
    const next = chain.value.map((e, i) => (i === index ? { ...e, model: value } : e))
    save(next)
}

function addRow() {
    const preferred =
        providerOptions.value.find((o) => o.value === 'microsoft')?.value ||
        providerOptions.value[0]?.value ||
        SERVICE_TYPE.LIBRETRANSLATE
    save([...chain.value, { id: crypto.randomUUID(), provider: preferred }])
}

function removeRow(index: number) {
    if (index === 0 || chain.value.length <= 1) return
    const next = chain.value.filter((_, i) => i !== index)
    save(next)
}

function moveRow(index: number, delta: number) {
    const target = index + delta
    if (target < 0 || target >= chain.value.length) return
    const next = [...chain.value]
    const tmp = next[index]
    next[index] = next[target]
    next[target] = tmp
    save(next)
}

async function loadModels(index: number, refresh: boolean) {
    const provider = chain.value[index]?.provider
    if (!provider || !supportsModel(provider)) return
    modelSelectRefs.value[index]?.setLoadingState(true)
    try {
        modelError[index] = null
        const endpoint = `/api/plugin/${provider}/models${refresh ? '?refresh=true' : ''}`
        const response = await servicesApi.plugin.getOptions(endpoint)
        modelOptions[index] = (response.options || []).map((o: any) => ({
            value: o.value,
            label: o.label
        }))
        if (!modelOptions[index].length) {
            modelError[index] = response.message || 'No models returned'
        }
    } catch (e) {
        console.error(e)
        modelError[index] = 'Error loading models'
    } finally {
        modelSelectRefs.value[index]?.setLoadingState(false)
    }
}

watch(
    () => settingsStore.getSetting(SETTINGS.SERVICE_TYPE),
    (raw) => {
        chain.value = parseChain(raw)
    }
)

onMounted(async () => {
    chain.value = parseChain(settingsStore.getSetting(SETTINGS.SERVICE_TYPE))
    try {
        const summaries: IPluginSummary[] = await servicesApi.plugin.list()
        instructionProviders.value = new Set(
            summaries
                .filter((summary) => summary.supportsInstructionProfiles)
                .map((summary) => summary.provider.toLowerCase())
        )
        providerOptions.value = summaries
            .map((s) => ({ value: s.provider, label: s.displayName }))
            .sort((a, b) => a.label.localeCompare(b.label))
    } catch (error) {
        console.error('Failed to load translation provider list', error)
    }
    try {
        const [profiles, activeSystem, activeContext] = await Promise.all([
            servicesApi.promptProfile.list(),
            servicesApi.setting.getSetting<string>(SETTINGS.ACTIVE_SYSTEM_PROMPT_PROFILE_ID),
            servicesApi.setting.getSetting<string>(SETTINGS.ACTIVE_CONTEXT_PROMPT_PROFILE_ID)
        ])
        promptProfiles.value = profiles
        activeSystemProfileId.value = Number(activeSystem) || 0
        activeContextProfileId.value = Number(activeContext) || 0
    } catch (error) {
        console.error('Failed to load prompt profiles', error)
    }
    // Ensure encrypted keys for providers on the chain are loaded into the store.
    const keys = [
        ...new Set(
            chain.value
                .map((e) => apiKeySettingKey(e.provider))
                .filter((k): k is keyof IEncryptedSettings => !!k)
        )
    ]
    if (keys.length) {
        try {
            await settingsStore.setPluginSettings(
                keys.map((key) => ({
                    key,
                    label: key,
                    type: PLUGIN_SETTING_TYPE.SECRET,
                    required: false
                })) as any
            )
        } catch {
            /* store may already hold keys from global load */
        }
    }
    chain.value.forEach((e, i) => {
        if (supportsModel(e.provider)) loadModels(i, false)
    })
})
</script>
