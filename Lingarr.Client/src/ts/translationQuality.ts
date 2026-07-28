export interface ITranslationQualitySummary {
    assessmentId: number
    translationRequestId: number
    score: number | null
    grade: string
    averageLineScore: number | null
    lowTailScore: number | null
    criticalCount: number
    errorCount: number
    warningCount: number
    lineCount: number
    evaluatedAt: string
    evaluationStatus: 'completed' | 'unavailable'
}

export interface ITranslationQualityFinding {
    id: number
    translationRequestLineId: number | null
    linePosition: number | null
    ruleId: string
    category: string
    severity: 'info' | 'warning' | 'error' | 'critical'
    penalty: number
    summary: string
    metadataJson: string
}

export interface ITranslationQualityDetail {
    summary: ITranslationQualitySummary | null
    findings: ITranslationQualityFinding[]
}
