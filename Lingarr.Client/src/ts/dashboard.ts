export interface IDashboardNamedCount {
    name: string
    count: number
}

export interface IDashboardActivityBucket {
    hour: string
    completedFiles: number
    translatedLines: number
}

export interface IDashboardActivity {
    windowHours: number
    windowStartedAt: string
    generatedAt: string
    completedFiles: number
    translatedLines: number
    activeTranslations: number
    failedTranslations: number
    qualityChecked: number
    qualityPassed: number
    qualityNeedsReview: number
    averageQualityScore: number | null
    fallbackRecoveries: number
    unavailableProviders: number
    topProviders: IDashboardNamedCount[]
    topLanguagePairs: IDashboardNamedCount[]
    buckets: IDashboardActivityBucket[]
    headline: string
    narrative: string[]
}
