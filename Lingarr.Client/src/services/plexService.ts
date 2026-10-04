import { AxiosResponse, AxiosStatic } from 'axios'
import {
    IPlexPin,
    IPlexPoll,
    IPlexServer,
    IPlexService,
    IPlexStatus,
    IPlexTest
} from '@/ts'

export const plexService = (http: AxiosStatic, resource = '/api/plex'): IPlexService => ({
    status(): Promise<IPlexStatus> {
        return http
            .get(`${resource}/status`)
            .then((response: AxiosResponse<IPlexStatus>) => response.data)
    },
    startPin(): Promise<IPlexPin> {
        return http
            .post(`${resource}/oauth/pin`)
            .then((response: AxiosResponse<IPlexPin>) => response.data)
    },
    pollPin(pinId: number): Promise<IPlexPoll> {
        return http
            .post(`${resource}/oauth/pin/${pinId}/poll`)
            .then((response: AxiosResponse<IPlexPoll>) => response.data)
    },
    servers(): Promise<IPlexServer[]> {
        return http
            .get(`${resource}/servers`)
            .then((response: AxiosResponse<IPlexServer[]>) => response.data)
    },
    selectServer(machineIdentifier: string, name: string, url: string): Promise<IPlexStatus> {
        return http
            .post(`${resource}/server`, { machineIdentifier, name, url })
            .then((response: AxiosResponse<IPlexStatus>) => response.data)
    },
    saveToken(url: string, token: string): Promise<IPlexStatus> {
        return http
            .post(`${resource}/token`, { url, token })
            .then((response: AxiosResponse<IPlexStatus>) => response.data)
    },
    test(): Promise<IPlexTest> {
        return http
            .post(`${resource}/test`)
            .then((response: AxiosResponse<IPlexTest>) => response.data)
    },
    logout(): Promise<void> {
        return http.delete(`${resource}/session`).then(() => undefined)
    }
})
