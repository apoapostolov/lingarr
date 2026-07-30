<template>
    <div class="rounded-lg bg-primary p-4 shadow-sm">
        <div class="flex justify-between">
            <div>
                <h3 class="text-primary-content/70 text-sm font-medium">
                    {{ title }}
                </h3>
                <p class="mt-2 text-2xl font-bold text-primary-content">
                    <AnimatedNumber :value="total" />
                </p>
            </div>
            <div class="text-right">
                <h3 class="text-primary-content/70 text-sm font-medium">{{ valueLabel }}</h3>
                <p class="mt-2 text-xl font-bold text-accent">
                    <AnimatedNumber :value="translated" />
                </p>
            </div>
        </div>
        <div class="mt-2 h-2 w-full overflow-hidden rounded-full bg-secondary">
            <div
                class="h-full rounded-full bg-accent transition-all duration-500"
                :style="{ width: `${calculatePercentage(translated, total)}%` }"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
import AnimatedNumber from '@/components/common/AnimatedNumber.vue'

interface Props {
    title: string
    total: number
    translated: number
    valueLabel?: string
}

withDefaults(defineProps<Props>(), {
    total: 0,
    translated: 0,
    valueLabel: 'Translated'
})

const calculatePercentage = (value: number, total: number): number => {
    if (total === 0) return 0
    const percentage = (value / total) * 100
    return Math.min(percentage, 100)
}
</script>
