<template>
    <div v-if="message" class="rounded-md border p-4" :class="[borderClass, bgClass]">
        <div class="flex items-start">
            <div v-if="showIcon" class="shrink-0">
                <CheckMarkCircleIcon v-if="type === 'success'" class="h-5 w-5" :class="iconClass" />
                <TimesCircleIcon v-else-if="type === 'error'" class="h-5 w-5" :class="iconClass" />
                <ExclamationIcon v-else class="h-5 w-5" :class="iconClass" />
            </div>
            <div :class="{ 'ml-3': showIcon }">
                <p class="text-sm" :class="textClass">{{ message }}</p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import CheckMarkCircleIcon from '@/components/icons/CheckMarkCircleIcon.vue'
import TimesCircleIcon from '@/components/icons/TimesCircleIcon.vue'
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'

type MessageType = 'success' | 'error' | 'warning' | 'info'

const props = withDefaults(
    defineProps<{
        message?: string
        type?: MessageType
        showIcon?: boolean
    }>(),
    {
        type: 'info',
        showIcon: false
    }
)

// Theme-native: secondary/tertiary surfaces with accent borders — no loud greens/reds/yellows.
const borderClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'border-accent/50'
        case 'error':
            return 'border-accent/40'
        case 'warning':
            return 'border-accent/45'
        case 'info':
        default:
            return 'border-accent/35'
    }
})

const bgClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'bg-secondary'
        case 'error':
            return 'bg-tertiary'
        case 'warning':
            return 'bg-secondary'
        case 'info':
        default:
            return 'bg-secondary/80'
    }
})

const textClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'text-accent-content'
        case 'error':
            return 'text-primary-content/90'
        case 'warning':
            return 'text-primary-content/85'
        case 'info':
        default:
            return 'text-primary-content/80'
    }
})

const iconClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'text-accent'
        case 'error':
            return 'text-primary-content/70'
        case 'warning':
            return 'text-accent-content'
        case 'info':
        default:
            return 'text-accent/80'
    }
})
</script>
