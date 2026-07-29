<template>
    <span :title="formatDateTime(completedDate)">
        {{ relativeTime }}
    </span>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { formatDateTime } from '@/utils/date'

const props = defineProps<{
    completedAt: string
}>()

const now = ref(Date.now())
let timer: ReturnType<typeof setInterval> | undefined

const completedDate = computed(() => {
    const hasTimeZone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(props.completedAt)
    return new Date(hasTimeZone ? props.completedAt : `${props.completedAt}Z`)
})

const relativeTime = computed(() => {
    const completed = completedDate.value.getTime()
    if (!Number.isFinite(completed)) return '—'

    const minutes = Math.max(0, Math.floor((now.value - completed) / 60_000))
    if (minutes < 1) return '< 1 m'
    if (minutes < 60) return `${minutes} m`

    const hours = Math.floor(minutes / 60)
    if (hours < 24) return `${hours} h`

    return `${Math.floor(hours / 24)} d`
})

onMounted(() => {
    timer = setInterval(() => {
        now.value = Date.now()
    }, 60_000)
})

onUnmounted(() => {
    if (timer) clearInterval(timer)
})
</script>
