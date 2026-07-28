import { AxiosStatic } from 'axios'
import {
    ICreatePromptProfile,
    IPromptProfile,
    IPromptProfileDeleteResult,
    IPromptProfileService,
    ISavePromptProfileDraft,
    PromptProfileType
} from '@/ts'

export const promptProfileService = (
    http: AxiosStatic,
    resource = '/api/instruction-profile'
): IPromptProfileService => ({
    async list(type?: PromptProfileType) {
        const response = await http.get<IPromptProfile[]>(resource, {
            params: type ? { type } : undefined
        })
        return response.data
    },
    async create(request: ICreatePromptProfile) {
        const response = await http.post<IPromptProfile>(resource, request)
        return response.data
    },
    async saveDraft(id: number, request: ISavePromptProfileDraft) {
        const response = await http.put<IPromptProfile>(`${resource}/${id}/draft`, request)
        return response.data
    },
    async publish(id: number, changeNote: string) {
        const response = await http.post<IPromptProfile>(`${resource}/${id}/publish`, {
            changeNote
        })
        return response.data
    },
    async restore(id: number, versionId: number) {
        const response = await http.post<IPromptProfile>(
            `${resource}/${id}/restore/${versionId}`
        )
        return response.data
    },
    async activate(id: number) {
        await http.post(`${resource}/${id}/activate`)
    },
    async delete(id: number) {
        const response = await http.delete<IPromptProfileDeleteResult>(`${resource}/${id}`)
        return response.data
    }
})
