<template>
    <Transition name="save-toast">
        <div
            v-if="isSaved"
            role="status"
            aria-live="polite"
            aria-atomic="true"
            class="save-toast border-accent bg-accent/25 text-accent-content absolute top-2 right-2 z-20 flex items-center rounded-full border px-3 py-1 text-xs font-semibold shadow-lg shadow-black/20">
            <svg
                class="mr-1 h-4 w-4"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
                aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <path d="m9 12 2 2 4-4" />
            </svg>
            Saved
        </div>
    </Transition>
</template>

<script lang="ts" setup>
import { onBeforeUnmount, ref } from 'vue'

const { duration = 2200, cooldown = 900 } = defineProps<{
    duration?: number
    cooldown?: number
}>()

const isSaved = ref(false)
let hideTimer: ReturnType<typeof setTimeout> | undefined
let cooldownTimer: ReturnType<typeof setTimeout> | undefined
let pending = false
let nextAllowedAt = 0

const display = () => {
    isSaved.value = true
    nextAllowedAt = Date.now() + duration + cooldown

    hideTimer = setTimeout(() => {
        isSaved.value = false
        hideTimer = undefined

        if (!pending) return

        pending = false
        const remainingCooldown = Math.max(0, nextAllowedAt - Date.now())
        cooldownTimer = setTimeout(() => {
            cooldownTimer = undefined
            display()
        }, remainingCooldown)
    }, duration)
}

const show = () => {
    if (isSaved.value || hideTimer || cooldownTimer || Date.now() < nextAllowedAt) {
        pending = true
        return
    }

    display()
}

onBeforeUnmount(() => {
    if (hideTimer) clearTimeout(hideTimer)
    if (cooldownTimer) clearTimeout(cooldownTimer)
})

defineExpose({
    show
})
</script>

<style scoped>
.save-toast-enter-active {
    animation: save-toast-spring-in 560ms both;
}

.save-toast-leave-active {
    transition:
        transform 120ms cubic-bezier(0.22, 1, 0.36, 1),
        opacity 120ms cubic-bezier(0.22, 1, 0.36, 1);
}

.save-toast-leave-to {
    opacity: 0;
    transform: translate3d(0, -4px, 0);
}

@keyframes save-toast-spring-in {
    0% {
        opacity: 0;
        transform: translate3d(0, -10px, 0) scale(0.96);
    }

    34% {
        opacity: 1;
        transform: translate3d(0, 7px, 0) scale(1.025);
    }

    58% {
        transform: translate3d(0, -4px, 0) scale(0.99);
    }

    76% {
        transform: translate3d(0, 2px, 0) scale(1.008);
    }

    90% {
        transform: translate3d(0, -1px, 0) scale(0.998);
    }

    100% {
        opacity: 1;
        transform: translate3d(0, 0, 0) scale(1);
    }
}

@media (prefers-reduced-motion: reduce) {
    .save-toast-enter-active,
    .save-toast-leave-active {
        animation: none;
        transition: opacity 100ms linear;
    }

    .save-toast-enter-from,
    .save-toast-leave-to {
        opacity: 0;
        transform: none;
    }
}
</style>
