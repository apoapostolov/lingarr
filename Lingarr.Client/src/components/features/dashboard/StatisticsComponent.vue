<template>
    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <CardComponent title="Media Overview" class="lg:col-span-2">
            <template #description>
                What Lingarr can see in your connected libraries and how much has been translated.
            </template>
            <template #content>
                <div v-if="loading" class="flex h-32 items-center justify-center">
                    <LoaderCircleIcon class="h-8 w-8 animate-spin" />
                </div>
                <div v-else-if="statistics" class="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <StatCard
                        title="Movies"
                        :total="statistics.totalMovies"
                        :translated="getTranslationCount(MEDIA_TYPE.MOVIE)" />
                    <StatCard
                        title="TV Episodes"
                        :total="statistics.totalEpisodes"
                        :translated="getTranslationCount(MEDIA_TYPE.EPISODE)" />
                </div>
                <p v-else class="py-8 text-center text-sm text-primary-content/60">
                    Media statistics are unavailable.
                </p>
            </template>
        </CardComponent>

        <ProviderHealthPanel />

        <CardComponent title="Recent Activity">
            <template #description>
                Work completed in the selected period, based on translation requests rather than
                lifetime totals.
            </template>
            <template #content>
                <div class="mb-4 flex items-center justify-between gap-3">
                    <span class="text-xs font-semibold tracking-wide text-primary-content/50 uppercase">
                        Time window
                    </span>
                    <select
                        v-model.number="windowHours"
                        aria-label="Dashboard activity time window"
                        class="rounded-md border border-accent bg-secondary px-3 py-2 text-sm text-primary-content"
                        @change="changeWindow">
                        <option :value="12">Last 12 hours</option>
                        <option :value="24">Last 24 hours</option>
                        <option :value="48">Last 48 hours</option>
                        <option :value="72">Last 3 days</option>
                        <option :value="168">Last 7 days</option>
                    </select>
                </div>

                <div v-if="activityLoading" class="flex h-48 items-center justify-center">
                    <LoaderCircleIcon class="h-8 w-8 animate-spin" />
                </div>
                <template v-else-if="activity">
                    <div class="grid grid-cols-2 gap-3">
                        <MetricCard title="Subtitle files" :value="activity.completedFiles" />
                        <MetricCard title="Dialogue lines" :value="activity.translatedLines" />
                        <MetricCard title="Still running" :value="activity.activeTranslations" />
                        <MetricCard title="Recovered by fallback" :value="activity.fallbackRecoveries" />
                    </div>

                    <div class="mt-5">
                        <div class="mb-2 flex items-center justify-between text-xs text-primary-content/50">
                            <span>Translation progress</span>
                            <span>{{ peakBucket }} lines at busiest point</span>
                        </div>
                        <div
                            class="flex h-24 items-end gap-1 rounded-md border border-accent/25 bg-primary/45 p-2"
                            role="img"
                            :aria-label="`Translated lines over the last ${windowHours} hours`">
                            <div
                                v-for="(bucket, index) in chartBuckets"
                                :key="index"
                                class="min-h-1 flex-1 rounded-t-sm bg-accent/75 transition-all duration-300"
                                :style="{ height: `${bucketHeight(bucket)}%` }"
                                :title="`${bucket.lines.toLocaleString()} lines, ${bucket.files} files`"></div>
                        </div>
                    </div>

                    <div v-if="activity.topProviders.length" class="mt-4">
                        <h3 class="mb-2 text-xs font-semibold tracking-wide text-primary-content/50 uppercase">
                            Most used providers
                        </h3>
                        <div class="flex flex-wrap gap-2">
                            <span
                                v-for="provider in activity.topProviders"
                                :key="provider.name"
                                class="rounded-full border border-accent/35 bg-accent/10 px-2.5 py-1 text-xs text-primary-content">
                                {{ provider.name }} · {{ provider.count.toLocaleString() }} lines
                            </span>
                        </div>
                    </div>
                </template>
                <p v-else class="py-8 text-center text-sm text-primary-content/60">
                    Recent activity is unavailable.
                </p>
            </template>
        </CardComponent>

        <CardComponent title="Subtitle Quality">
            <template #description>
                Mechanical checks for completed subtitles in this same time window.
            </template>
            <template #content>
                <template v-if="activity">
                    <div class="grid grid-cols-3 gap-3">
                        <MetricCard title="Checked" :value="activity.qualityChecked" />
                        <MetricCard title="Passed" :value="activity.qualityPassed" />
                        <MetricCard title="Needs review" :value="activity.qualityNeedsReview" />
                    </div>
                    <div class="mt-5 rounded-md border border-accent/25 bg-primary/45 p-4">
                        <div class="text-sm text-primary-content/60">Average quality score</div>
                        <div class="mt-1 text-3xl font-bold text-primary-content">
                            {{ activity.averageQualityScore ?? '—' }}
                            <span class="text-sm font-normal text-primary-content/45">/ 100</span>
                        </div>
                        <p class="mt-2 text-xs text-primary-content/50">
                            This reflects suspicious mechanical results, not artistic translation
                            quality.
                        </p>
                    </div>
                    <div v-if="activity.topLanguagePairs.length" class="mt-4 space-y-2">
                        <div
                            v-for="pair in activity.topLanguagePairs"
                            :key="pair.name"
                            class="flex items-center justify-between rounded-md bg-primary/45 px-3 py-2 text-sm">
                            <span class="text-primary-content">{{ pair.name }}</span>
                            <span class="text-primary-content/55">
                                {{ pair.count }} {{ pair.count === 1 ? 'file' : 'files' }}
                            </span>
                        </div>
                    </div>
                </template>
            </template>
        </CardComponent>

        <CardComponent title="Progress Summary">
            <template #description>
                A deterministic plain-language explanation of what Lingarr has done.
            </template>
            <template #content>
                <template v-if="activity">
                    <div
                        class="rounded-md border border-accent/35 bg-accent/10 p-4 text-lg font-semibold text-primary-content">
                        {{ activity.headline }}
                    </div>
                    <ul class="mt-4 space-y-3">
                        <li
                            v-for="sentence in activity.narrative"
                            :key="sentence"
                            class="flex items-start gap-3 text-sm leading-6 text-primary-content/75">
                            <span class="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-accent"></span>
                            <span>{{ sentence }}</span>
                        </li>
                    </ul>
                </template>
            </template>
        </CardComponent>

        <CardComponent title="All-time Totals" class="lg:col-span-2">
            <template #description>
                Historical context. These figures are secondary to the recent operational view.
            </template>
            <template #content>
                <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
                    <MetricCard
                        title="Lines translated"
                        :value="statistics?.totalLinesTranslated ?? 0" />
                    <MetricCard
                        title="Files processed"
                        :value="statistics?.totalFilesTranslated ?? 0" />
                    <MetricCard
                        title="Characters translated"
                        :value="statistics?.totalCharactersTranslated ?? 0" />
                </div>
            </template>
        </CardComponent>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { IDashboardActivity, MEDIA_TYPE, SETTINGS, Statistics } from '@/ts'
import services from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import ProviderHealthPanel from '@/components/features/providerHealth/ProviderHealthPanel.vue'
import MetricCard from './MetricCard.vue'
import StatCard from './StatCard.vue'

const loading = ref(true)
const activityLoading = ref(true)
const statistics = ref<Statistics>()
const activity = ref<IDashboardActivity>()
const windowHours = ref(48)

const getTranslationCount = (type: string): number =>
    statistics.value?.translationsByMediaType?.[type] || 0

const chartBuckets = computed(() => {
    const source = activity.value?.buckets ?? []
    if (source.length <= 24) {
        return source.map((bucket) => ({
            files: bucket.completedFiles,
            lines: bucket.translatedLines
        }))
    }
    const chunkSize = Math.ceil(source.length / 24)
    const result: { files: number; lines: number }[] = []
    for (let index = 0; index < source.length; index += chunkSize) {
        const chunk = source.slice(index, index + chunkSize)
        result.push({
            files: chunk.reduce((sum, item) => sum + item.completedFiles, 0),
            lines: chunk.reduce((sum, item) => sum + item.translatedLines, 0)
        })
    }
    return result
})
const peakBucket = computed(() =>
    Math.max(0, ...chartBuckets.value.map((bucket) => bucket.lines)).toLocaleString()
)
const bucketHeight = (bucket: { lines: number }) => {
    const peak = Math.max(1, ...chartBuckets.value.map((item) => item.lines))
    return Math.max(4, Math.round((bucket.lines / peak) * 100))
}

const fetchActivity = async (hours?: number) => {
    activityLoading.value = true
    try {
        activity.value = await services.dashboard.activity(hours)
        windowHours.value = activity.value.windowHours
    } finally {
        activityLoading.value = false
    }
}

const changeWindow = async () => {
    await services.setting.setSetting(
        SETTINGS.DASHBOARD_ACTIVITY_WINDOW_HOURS,
        String(windowHours.value)
    )
    await fetchActivity(windowHours.value)
}

onMounted(async () => {
    const [stats] = await Promise.allSettled([
        services.statistics.getStatistics<Statistics>(),
        fetchActivity()
    ])
    if (stats.status === 'fulfilled') statistics.value = stats.value
    loading.value = false
})
</script>
