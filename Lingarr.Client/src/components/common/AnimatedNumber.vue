<template>
    <span class="tabular-nums">{{ display }}</span>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'

const props = withDefaults(
    defineProps<{
        value: number
        /** Animation length in milliseconds. */
        duration?: number
        /** Fractional digits to render. */
        decimals?: number
    }>(),
    {
        duration: 650,
        decimals: 0
    }
)

const formatter = (): Intl.NumberFormat =>
    new Intl.NumberFormat(undefined, {
        minimumFractionDigits: props.decimals,
        maximumFractionDigits: props.decimals
    })

// The numeric value currently shown. Kept as a number so we can resume a
// tween from the exact in-flight value when the prop changes mid-animation.
const current = ref(props.value)
const display = computed(() => formatter().format(current.value))

let from = props.value
let to = props.value
let startTime = 0
let frame = 0

const easeOutCubic = (t: number): number => 1 - Math.pow(1 - t, 3)

const tick = (time: number): void => {
    if (!startTime) startTime = time
    const progress = Math.min((time - startTime) / props.duration, 1)
    current.value = from + (to - from) * easeOutCubic(progress)
    if (progress < 1) {
        frame = requestAnimationFrame(tick)
    } else {
        current.value = to
        from = to
    }
}

// Animate whenever the target value changes after mount. On first render the
// value is shown as-is (no tween from zero), so cached data appears instantly.
watch(
    () => props.value,
    (next) => {
        if (next === to) return
        cancelAnimationFrame(frame)
        from = current.value
        to = next
        startTime = 0
        frame = requestAnimationFrame(tick)
    }
)

onBeforeUnmount(() => cancelAnimationFrame(frame))
</script>
