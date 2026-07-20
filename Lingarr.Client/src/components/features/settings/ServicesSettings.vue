<template>
    <CardComponent title="Services">
        <template #description>
            Configure the translation service for subtitle localization. Add fallback services to be
            tried in order when the primary service fails. AI providers include a model selector on
            each row.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />

            <div class="space-y-2">
                <span class="font-semibold">Translation services</span>
                <ol class="space-y-2">
                    <li
                        v-for="(entry, index) in chain"
                        :key="`row-${index}-${entry.provider}`"
                        class="flex items-center gap-3 rounded-md border border-accent/30 p-3"
                        :class="{ 'border-accent bg-accent/10': index === configuringIndex }"
                        @click="configuringIndex = index">
                        <!-- 1-based index badge (primary = 1) -->
                        <span
                            class="bg-accent/20 text-accent-content flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-sm font-semibold tabular-nums"
                            :title="index === 0 ? 'Primary' : `Fallback ${index}`"
                            :aria-label="index === 0 ? 'Primary' : `Fallback ${index}`">
                            {{ index + 1 }}
                        </span>

                        <div class="flex min-w-0 flex-1 flex-col gap-2 sm:flex-row sm:items-center">
                            <div class="min-w-0 flex-1" @click.stop>
                                <SelectComponent
                                    :selected="entry.provider"
                                    :options="providerOptions"
                                    placeholder="Select provider..."
                                    @update:selected="(value: string) => setProvider(index, value)" />
                            </div>
                            <div
                                v-if="supportsModel(entry.provider)"
                                class="flex min-w-0 flex-1 items-center gap-2"
                                @click.stop>
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
                        </div>

                        <div class="flex shrink-0 items-center gap-0.5" @click.stop>
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

            <div v-if="credentialsManifest || manifestError" class="mt-4 space-y-2">
                <div class="text-sm">
                    <span class="text-secondary-content/60">Configuring credentials for</span>
                    <span class="ml-1 font-semibold">{{ configuringLabel }}</span>
                    <span class="text-secondary-content/60 ml-1">(row {{ configuringIndex + 1 }})</span>
                </div>
                <DynamicPluginForm
                    v-if="credentialsManifest"
                    :manifest="credentialsManifest"
                    @save="saveNotification?.show()" />
                <p v-else-if="manifestError" class="text-sm text-red-500">{{ manifestError }}</p>
            </div>

            <div v-if="configuringManifest?.hasRequestTemplate" class="mt-6">
                <div class="flex flex-col gap-4">
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">Customize request template and prompts</span>
                        Adjust the AI request body, system prompt and context for translations.
                    </div>
                    <ButtonComponent
                        variant="primary"
                        size="md"
                        @click="
                            router.push({
                                name: 'request-template-settings',
                                params: { service: chain[configuringIndex]?.provider }
                            })
                        ">
                        Open Request Settings
                        <ArrowRight class="mt-1 ml-1 h-4 w-4" />
                    </ButtonComponent>
                </div>
            </div>

            <SourceAndTarget @save="saveNotification?.show()" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingStore } from '@/store/setting'
import {
    IPluginManifest,
    IPluginSummary,
    PLUGIN_SETTING_TYPE,
    SETTINGS,
    SERVICE_TYPE,
    SelectComponentExpose
} from '@/ts'
import servicesApi from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import DynamicPluginForm from '@/components/features/settings/DynamicPluginForm.vue'
import SourceAndTarget from '@/components/features/settings/SourceAndTarget.vue'
import ArrowRight from '@/components/icons/ArrowRight.vue'
import CaretUpIcon from '@/components/icons/CaretUpIcon.vue'
import CaretDownIcon from '@/components/icons/CaretDownIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import PlusIcon from '@/components/icons/PlusIcon.vue'

export type ChainEntry = { provider: string; model?: string | null }

const MODEL_PROVIDERS = new Set([
    'openai',
    'anthropic',
    'gemini',
    'deepseek',
    'localai',
    'openrouter',
    'zai',
    'opencode-go'
])

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const router = useRouter()

const providerOptions = ref<{ value: string; label: string }[]>([])
const chain = ref<ChainEntry[]>([{ provider: SERVICE_TYPE.LIBRETRANSLATE }])
const configuringIndex = ref(0)
const configuringManifest = ref<IPluginManifest | null>(null)
const manifestError = ref<string | null>(null)
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

function isModelField(key: string, type: string): boolean {
    if (type === PLUGIN_SETTING_TYPE.REMOTE_DROPDOWN) return true
    const k = key.toLowerCase()
    return k.endsWith('_model') || k.includes('_model') || k === 'model'
}

const credentialsManifest = computed<IPluginManifest | null>(() => {
    const manifest = configuringManifest.value
    if (!manifest) return null
    const provider = chain.value[configuringIndex.value]?.provider
    if (!supportsModel(provider)) return manifest
    return {
        ...manifest,
        settings: manifest.settings.filter((field) => !isModelField(field.key, field.type))
    }
})

function parseChain(raw: unknown): ChainEntry[] {
    try {
        const text = (raw as string) ?? '[]'
        if (!text.trim().startsWith('[')) {
            return [{ provider: text.trim() || SERVICE_TYPE.LIBRETRANSLATE }]
        }
        const parsed = JSON.parse(text) as unknown[]
        if (!Array.isArray(parsed) || parsed.length === 0) {
            return [{ provider: SERVICE_TYPE.LIBRETRANSLATE }]
        }
        return parsed.map((item) => {
            if (typeof item === 'string') return { provider: item }
            const obj = item as { provider?: string; service?: string; model?: string }
            return {
                provider: obj.provider || obj.service || SERVICE_TYPE.LIBRETRANSLATE,
                model: obj.model || null
            }
        })
    } catch {
        return [{ provider: SERVICE_TYPE.LIBRETRANSLATE }]
    }
}

async function save(next: ChainEntry[]) {
    chain.value = next
    const payload = next.map((e) =>
        e.model ? { provider: e.provider, model: e.model } : { provider: e.provider }
    )
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
        'opencode-go': (SETTINGS as any).OPENCODE_GO_MODEL
    }
    return map[provider.toLowerCase()] ?? null
}

function setProvider(index: number, value: string) {
    const next = chain.value.map((e, i) =>
        i === index ? { provider: value, model: supportsModel(value) ? e.model : null } : e
    )
    save(next)
    configuringIndex.value = index
    loadModels(index, false)
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
    save([...chain.value, { provider: preferred }])
}

function removeRow(index: number) {
    if (index === 0 || chain.value.length <= 1) return
    const next = chain.value.filter((_, i) => i !== index)
    save(next)
    configuringIndex.value = Math.min(configuringIndex.value, next.length - 1)
}

function moveRow(index: number, delta: number) {
    const target = index + delta
    if (target < 0 || target >= chain.value.length) return
    const next = [...chain.value]
    const tmp = next[index]
    next[index] = next[target]
    next[target] = tmp
    if (configuringIndex.value === index) configuringIndex.value = target
    else if (configuringIndex.value === target) configuringIndex.value = index
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

async function loadManifest(provider: string) {
    try {
        const manifest = await servicesApi.plugin.getManifest(provider)
        await settingsStore.setPluginSettings(manifest.settings)
        configuringManifest.value = manifest
        manifestError.value = null
    } catch (error) {
        console.error('Failed to load manifest', error)
        configuringManifest.value = null
        manifestError.value = `No manifest available for ${provider}.`
    }
}

const configuringLabel = computed(() => {
    const value = chain.value[configuringIndex.value]?.provider
    return providerOptions.value.find((o) => o.value === value)?.label ?? value
})

watch(
    () => settingsStore.getSetting(SETTINGS.SERVICE_TYPE),
    (raw) => {
        chain.value = parseChain(raw)
    }
)

watch(
    () => chain.value[configuringIndex.value]?.provider,
    (provider) => {
        if (provider) loadManifest(provider)
    },
    { immediate: true }
)

onMounted(async () => {
    chain.value = parseChain(settingsStore.getSetting(SETTINGS.SERVICE_TYPE))
    try {
        const summaries: IPluginSummary[] = await servicesApi.plugin.list()
        providerOptions.value = summaries
            .map((s) => ({ value: s.provider, label: s.displayName }))
            .sort((a, b) => a.label.localeCompare(b.label))
    } catch (error) {
        console.error('Failed to load translation provider list', error)
    }
    chain.value.forEach((e, i) => {
        if (supportsModel(e.provider)) loadModels(i, false)
    })
})
</script>
