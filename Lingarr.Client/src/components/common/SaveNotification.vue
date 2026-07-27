<template>
    <div
        v-if="isSaved"
        class="absolute right-2 top-2 flex items-center rounded-full border border-accent/50 bg-secondary px-2 py-1 text-xs font-medium text-accent-content shadow-sm">
        <svg
            class="mr-1 h-4 w-4 text-accent"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round">
            <circle cx="12" cy="12" r="10" />
            <path d="m9 12 2 2 4-4" />
        </svg>
        saved
    </div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'

const { duration = 2000 } = defineProps<{
    duration?: number
}>()

const isSaved = ref(false)

const show = () => {
    isSaved.value = true
    setTimeout(() => {
        isSaved.value = false
    }, duration)
}

defineExpose({
    show
})
</script>
