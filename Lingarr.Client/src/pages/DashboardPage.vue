<template>
    <div class="p-4">
        <StatisticsComponent />
        <ProviderHealthPanel class="mt-4" />
    </div>
</template>

<script setup lang="ts">
import StatisticsComponent from '@/components/features/dashboard/StatisticsComponent.vue'
import ProviderHealthPanel from '@/components/features/providerHealth/ProviderHealthPanel.vue'
</script>
