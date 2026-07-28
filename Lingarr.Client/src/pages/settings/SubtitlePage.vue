<template>
    <div class="w-full">
        <SettingsSectionTabs section="translation" />
        <div
            class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2 xl:grid-cols-2 2xl:grid-cols-3">
            <SubtitleSettings />
            <ValidationSettings />
        </div>
    </div>
</template>

<script setup lang="ts">
import SettingsSectionTabs from '@/components/features/settings/SettingsSectionTabs.vue'
import SubtitleSettings from '@/components/features/settings/SubtitleSettings.vue'
import ValidationSettings from '@/components/features/settings/ValidationSettings.vue'
</script>
