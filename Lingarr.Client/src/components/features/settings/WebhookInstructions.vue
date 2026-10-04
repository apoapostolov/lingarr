<template>
    <CardComponent title="Webhook">
        <template #description>
            Configure webhooks in Radarr, Sonarr, and Plex to trigger translations when new
            media is added.
        </template>
        <template #content>
            <div class="flex flex-col space-y-2">
                <span>
                    In Radarr or Sonarr, go to
                    <b>Settings → Connect → Connections → +</b>
                    and use this URL:
                </span>
                <span class="font-semibold">Radarr</span>
                <CodeSnippet class="mt-1 block overflow-x-auto">{{ radarrWebhookUrl }}</CodeSnippet>
                <span class="font-semibold">Sonarr</span>
                <CodeSnippet class="mt-1 block overflow-x-auto">{{ sonarrWebhookUrl }}</CodeSnippet>
                <span class="font-semibold">Plex</span>
                <span>
                    Settings → Webhooks. Lingarr handles a newly added movie or episode when a
                    source subtitle file is already there and a target subtitle is missing.
                </span>
                <CodeSnippet class="mt-1 block overflow-x-auto">{{ plexWebhookUrl }}</CodeSnippet>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import CardComponent from '@/components/common/CardComponent.vue'
import CodeSnippet from '@/components/common/CodeSnippet.vue'
import { resolveUrl } from '@/utils/baseUrl'

const radarrWebhookUrl = resolveUrl('/api/webhook/radarr')
const sonarrWebhookUrl = resolveUrl('/api/webhook/sonarr')
const plexWebhookUrl = resolveUrl('/api/webhook/plex')
</script>
