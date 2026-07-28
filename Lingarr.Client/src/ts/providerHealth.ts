export type ProviderHealthState =
    | 'not_configured'
    | 'not_checked'
    | 'healthy'
    | 'needs_attention'
    | 'recently_unavailable'
    | 'unavailable'

export interface IProviderHealth {
    provider: string
    displayName: string
    model?: string | null
    configured: boolean
    missingFields: string[]
    state: ProviderHealthState
    statusLabel: string
    reason: string
    lastSuccessAt?: string | null
    lastFailureAt?: string | null
    lastWarningAt?: string | null
    consecutiveFailures: number
    successCount: number
    failureCount: number
    successRate?: number | null
    medianDurationMs?: number | null
    evaluatedAt: string
}

export interface IProviderProbe {
    provider: string
    model?: string | null
    sourceLanguage: string
    targetLanguage: string
    supported: boolean
    success: boolean
    durationMs: number
    errorFamily?: string | null
    message: string
}
