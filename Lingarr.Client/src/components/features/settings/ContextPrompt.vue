<template>
    <CardComponent title="Context Prompt Profiles">
        <template #description>
            Templates that place the current subtitle line among neighbouring lines so an AI can
            understand who or what is being discussed.
        </template>
        <template #content>
            <div class="space-y-5">
                <SaveNotification ref="saveNotification" />
                <div class="rounded-md border border-accent/30 bg-primary/45 p-3">
                    <div class="flex flex-wrap items-center justify-between gap-3">
                        <div>
                            <div class="text-sm font-semibold text-primary-content">
                                Use neighbouring subtitle context
                            </div>
                            <p class="mt-1 text-xs text-primary-content/55">
                                Applies to individual-line AI translation. Batch translation already
                                carries several lines together.
                            </p>
                        </div>
                        <ToggleButton v-model="aiContextPromptEnabled">
                            <span class="text-sm font-medium text-primary-content">
                                {{ aiContextPromptEnabled === 'true' ? 'Enabled' : 'Disabled' }}
                            </span>
                        </ToggleButton>
                    </div>
                    <div
                        v-if="aiContextPromptEnabled === 'true'"
                        class="mt-4 grid gap-3 sm:grid-cols-2">
                        <InputComponent
                            v-model="contextBefore"
                            :type="INPUT_TYPE.NUMBER"
                            :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                            label="Lines before"
                            @update:validation="(value) => (isValid.contextBefore = value)" />
                        <InputComponent
                            v-model="contextAfter"
                            :type="INPUT_TYPE.NUMBER"
                            :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                            label="Lines after"
                            @update:validation="(value) => (isValid.contextAfter = value)" />
                    </div>
                    <p v-if="useBatchTranslation === 'true'" class="mt-3 text-xs text-yellow-200/80">
                        Context profiles are saved, but are not used while batch translation is
                        enabled.
                    </p>
                </div>

                <PromptProfileEditor
                    type="context"
                    title="Context Prompt"
                    :help="help"
                    :recommended-example="RECOMMENDED_CONTEXT_PROMPT" />
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { INPUT_TYPE, INPUT_VALIDATION_TYPE, SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import PromptProfileEditor from '@/components/features/settings/PromptProfileEditor.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import { RECOMMENDED_CONTEXT_PROMPT } from '@/components/features/settings/promptExamples'

const settingsStore = useSettingStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const isValid = reactive({ contextBefore: true, contextAfter: true })
const help =
    'The Context Prompt is a wrapper for one target line and its neighbours. Keep {lineToTranslate}; otherwise Lingarr cannot place the line to translate. The recommended tagged format clearly separates earlier context, the target, and later context so the model does not translate or repeat neighbouring lines.'

const useBatchTranslation = computed(
    () => settingsStore.getSetting(SETTINGS.USE_BATCH_TRANSLATION) as string
)
const aiContextPromptEnabled = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_CONTEXT_PROMPT_ENABLED) as string,
    set: (value: string) => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_PROMPT_ENABLED, value, true)
        saveNotification.value?.show()
    }
})
const contextBefore = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_CONTEXT_BEFORE) as string,
    set: (value: string) => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_BEFORE, value, isValid.contextBefore)
        if (isValid.contextBefore) saveNotification.value?.show()
    }
})
const contextAfter = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_CONTEXT_AFTER) as string,
    set: (value: string) => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_AFTER, value, isValid.contextAfter)
        if (isValid.contextAfter) saveNotification.value?.show()
    }
})
</script>
