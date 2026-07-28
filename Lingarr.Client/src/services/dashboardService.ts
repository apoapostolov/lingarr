import { AxiosStatic } from 'axios'
import { IDashboardActivity, IDashboardService } from '@/ts'

export const dashboardService = (
    http: AxiosStatic,
    resource = '/api/dashboard'
): IDashboardService => ({
    async activity(hours?: number): Promise<IDashboardActivity> {
        const response = await http.get<IDashboardActivity>(`${resource}/activity`, {
            params: hours ? { hours } : undefined
        })
        return response.data
    }
})
