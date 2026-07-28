<template>
    <CardComponent title="System Prompt Profiles">
        <template #description>
            Reusable translation behaviour for AI providers: voice, glossary, profanity, protected
            terms, and output rules.
        </template>
        <template #content>
            <PromptProfileEditor
                type="system"
                title="System Prompt"
                :help="help"
                :recommended-example="RECOMMENDED_SYSTEM_PROMPT" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import CardComponent from '@/components/common/CardComponent.vue'
import PromptProfileEditor from '@/components/features/settings/PromptProfileEditor.vue'
import { RECOMMENDED_SYSTEM_PROMPT } from '@/components/features/settings/promptExamples'

const help =
    'The System Prompt tells an AI translator how to behave for every line: tone, faithfulness, glossary, names, profanity, and formatting. Save a draft while editing; publish only when it is ready for new translations. Existing and in-progress translations keep the exact published version they started with.'
</script>
