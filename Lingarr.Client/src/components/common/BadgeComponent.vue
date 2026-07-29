<template>
    <div
        class="inline-flex items-center whitespace-nowrap border text-xs font-semibold"
        :class="[
            shape === 'square'
                ? 'h-8 min-w-8 justify-center rounded-md px-2'
                : 'rounded-full px-3 py-1',
            classes
        ]">
        <span><slot></slot></span>
    </div>
</template>

<script setup lang="ts">
const {
    classes = 'cursor-pointer text-primary-content border-accent bg-secondary',
    shape = 'pill'
} = defineProps<{
    classes?: string
    shape?: 'pill' | 'square'
}>()
</script>
