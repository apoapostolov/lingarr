<template>
    <div class="w-full">
        <SettingsSectionTabs section="translation" />
        <div class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4">
            <SystemPrompt />
            <ContextPrompt />
        </div>
    </div>
</template>

<script setup lang="ts">
import SettingsSectionTabs from '@/components/features/settings/SettingsSectionTabs.vue'
import SystemPrompt from '@/components/features/settings/SystemPrompt.vue'
import ContextPrompt from '@/components/features/settings/ContextPrompt.vue'
</script>
