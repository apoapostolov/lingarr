import { AxiosResponse, AxiosStatic } from 'axios'
import { IProviderHealth, IProviderHealthService, IProviderProbe } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/provider-health'): IProviderHealthService => ({
    list(): Promise<IProviderHealth[]> {
        return http
            .get(resource)
            .then((response: AxiosResponse<IProviderHealth[]>) => response.data)
    },
    test(provider: string): Promise<IProviderProbe> {
        return http
            .post(`${resource}/${encodeURIComponent(provider)}/test`)
            .then((response: AxiosResponse<IProviderProbe>) => response.data)
    }
})

export const providerHealthService = (axios: AxiosStatic): IProviderHealthService => service(axios)
