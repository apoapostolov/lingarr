export type PromptProfileType = 'system' | 'context'

export interface IPromptProfileVersion {
    id: number
    versionNumber: number
    content: string
    changeNote: string
    contentHash: string
    createdAt: string
}

export interface IPromptProfile {
    id: number
    type: PromptProfileType
    name: string
    description: string
    draftContent: string
    currentPublishedVersionId: number | null
    currentVersionNumber: number | null
    hasUnpublishedChanges: boolean
    isArchived: boolean
    assignmentCount: number
    updatedAt: string
    versions: IPromptProfileVersion[]
}

export interface ICreatePromptProfile {
    type: PromptProfileType
    name: string
    description: string
    content: string
}

export interface ISavePromptProfileDraft {
    name: string
    description: string
    content: string
}

export interface IPromptProfileDeleteResult {
    archived: boolean
}
