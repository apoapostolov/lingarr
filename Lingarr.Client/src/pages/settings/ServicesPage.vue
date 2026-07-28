<template>
    <div class="w-full">
        <SettingsSectionTabs section="translation" />
        <div class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2">
            <ServicesSettings />
            <LanguageSettings />
            <ProviderHealthPanel class="md:col-span-2" />
        </div>
    </div>
</template>

<script setup lang="ts">
import LanguageSettings from '@/components/features/settings/LanguageSettings.vue'
import ServicesSettings from '@/components/features/settings/ServicesSettings.vue'
import SettingsSectionTabs from '@/components/features/settings/SettingsSectionTabs.vue'
import ProviderHealthPanel from '@/components/features/providerHealth/ProviderHealthPanel.vue'
</script>
