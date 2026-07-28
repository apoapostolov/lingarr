<template>
    <nav :aria-label="`${sectionLabel} settings`" class="bg-tertiary px-4">
        <ul class="flex min-w-0 items-stretch overflow-x-auto">
            <li v-for="item in items" :key="item.route" class="shrink-0">
                <router-link
                    :to="{ name: item.route }"
                    class="focus-visible:ring-accent flex min-h-12 items-center border-b-2 px-4 text-sm font-medium transition-colors duration-200 focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-inset"
                    :class="
                        item.activeRoutes.includes(route.name as string)
                            ? 'border-accent text-primary-content'
                            : 'text-secondary-content/60 hover:text-primary-content border-transparent'
                    ">
                    {{ item.label }}
                </router-link>
            </li>
        </ul>
    </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

type SettingsSection = 'connections' | 'translation' | 'system'

type SettingsTab = {
    label: string
    route: string
    activeRoutes: string[]
}

const props = defineProps<{
    section: SettingsSection
}>()

const route = useRoute()

const sections: Record<SettingsSection, { label: string; items: SettingsTab[] }> = {
    connections: {
        label: 'Connections',
        items: [
            {
                label: 'Media servers',
                route: 'connections-media-settings',
                activeRoutes: ['connections-media-settings']
            },
            {
                label: 'Path mapping',
                route: 'connections-mapping-settings',
                activeRoutes: ['connections-mapping-settings']
            }
        ]
    },
    translation: {
        label: 'Translation',
        items: [
            {
                label: 'Setup',
                route: 'translation-setup-settings',
                activeRoutes: ['translation-setup-settings']
            },
            {
                label: 'Subtitles',
                route: 'translation-subtitles-settings',
                activeRoutes: ['translation-subtitles-settings']
            },
            {
                label: 'Advanced',
                route: 'translation-advanced-settings',
                activeRoutes: ['translation-advanced-settings', 'request-template-settings']
            }
        ]
    },
    system: {
        label: 'System',
        items: [
            {
                label: 'Access',
                route: 'system-access-settings',
                activeRoutes: ['system-access-settings']
            },
            {
                label: 'Tasks',
                route: 'system-tasks-settings',
                activeRoutes: ['system-tasks-settings']
            },
            {
                label: 'Logs',
                route: 'system-logs-settings',
                activeRoutes: ['system-logs-settings']
            }
        ]
    }
}

const sectionLabel = computed(() => sections[props.section].label)
const items = computed(() => sections[props.section].items)
</script>
