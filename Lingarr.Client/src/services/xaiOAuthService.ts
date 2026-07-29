import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IXaiOAuthDevice, IXaiOAuthPoll, IXaiOAuthService, IXaiOAuthStatus } from '@/ts'

export const xaiOAuthService = (
    http: AxiosStatic,
    resource = '/api/xai-oauth'
): IXaiOAuthService => ({
    status(): Promise<IXaiOAuthStatus> {
        return http
            .get(resource + '/status')
            .then((response: AxiosResponse<IXaiOAuthStatus>) => response.data)
            .catch((error: AxiosError) => Promise.reject(error.response))
    },
    start(): Promise<IXaiOAuthDevice> {
        return http
            .post(resource + '/device')
            .then((response: AxiosResponse<IXaiOAuthDevice>) => response.data)
            .catch((error: AxiosError) => Promise.reject(error.response))
    },
    poll(flowId: string): Promise<IXaiOAuthPoll> {
        return http
            .post(`${resource}/device/${encodeURIComponent(flowId)}/poll`)
            .then((response: AxiosResponse<IXaiOAuthPoll>) => response.data)
            .catch((error: AxiosError) => Promise.reject(error.response))
    },
    disconnect(): Promise<void> {
        return http
            .delete(resource + '/session')
            .then(() => undefined)
            .catch((error: AxiosError) => Promise.reject(error.response))
    }
})
