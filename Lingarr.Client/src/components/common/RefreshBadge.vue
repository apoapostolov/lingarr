<template>
    <span
        v-if="refreshing || fetchedAt"
        class="inline-flex items-center gap-1.5 text-xs font-medium text-primary-content/45"
        :title="refreshing ? 'Refreshing…' : 'Last updated'">
        <LoaderCircleIcon
            v-if="refreshing"
            class="h-3.5 w-3.5 animate-spin" />
        {{ refreshing ? 'Updating…' : `Updated ${relativeLabel}` }}
    </span>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'

const props = defineProps<{
    refreshing: boolean
    fetchedAt?: number
}>()

// Re-evaluate the relative label periodically so "Updated 1m ago" stays fresh
// without requiring a data refresh.
const now = ref(Date.now())
let timer = 0

onMounted(() => {
    timer = window.setInterval(() => {
        now.value = Date.now()
    }, 30_000)
})

onBeforeUnmount(() => {
    if (timer) window.clearInterval(timer)
})

const relativeLabel = computed(() => {
    if (!props.fetchedAt) return ''
    const seconds = Math.max(0, Math.round((now.value - props.fetchedAt) / 1000))
    if (seconds < 5) return 'just now'
    if (seconds < 60) return `${seconds}s ago`
    const minutes = Math.round(seconds / 60)
    if (minutes < 60) return `${minutes}m ago`
    const hours = Math.round(minutes / 60)
    if (hours < 24) return `${hours}h ago`
    const days = Math.round(hours / 24)
    return `${days}d ago`
})
</script>
