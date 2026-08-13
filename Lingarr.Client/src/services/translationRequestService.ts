import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import {
    ITranslationQualityDetail,
    ITranslationQualitySummary,
    ITranslationRequest,
    ITranslationRequestService
} from '@/ts'

const service = (
    http: AxiosStatic,
    resource = '/api/translationRequest'
): ITranslationRequestService => ({
    get<T>(id: number): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/${id}`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    getActiveTranslations<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/active`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    requests<T>(
        pageNumber: number,
        searchQuery: string,
        orderBy: string,
        ascending: boolean
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/requests`.addParams({
                    pageNumber: pageNumber,
                    searchQuery: searchQuery,
                    orderBy: orderBy,
                    ascending: ascending
                })
            )
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    cancel<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/cancel`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    remove<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/remove`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    retry<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/retry`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    resume<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/resume`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    proofread<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/proofread`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    async quality(id: number): Promise<ITranslationQualityDetail> {
        const response = await http.get<ITranslationQualityDetail>(`${resource}/${id}/quality`)
        return response.data
    },
    async reEvaluateQuality(id: number): Promise<ITranslationQualitySummary> {
        const response = await http.post<ITranslationQualitySummary>(
            `${resource}/${id}/quality/re-evaluate`
        )
        return response.data
    }
})

export const translationRequestService = (axios: AxiosStatic): ITranslationRequestService => {
    return service(axios)
}
