import { useLocalStorage } from '@/composables/useLocalStorage'
import { ref, type Ref } from 'vue'

export interface CachedResource<T> {
    /** The current value: hydrated from cache immediately, then updated on refresh. */
    data: Ref<T | undefined>
    /** True only until we have *any* data to show (no cache and no successful fetch yet). */
    loading: Ref<boolean>
    /** True while a background refresh is in flight. Use to show a subtle indicator. */
    refreshing: Ref<boolean>
    /** Last fetch error, if any. Stale data remains available in `data`. */
    error: Ref<unknown>
    /** Epoch milliseconds of the last successful fetch (cache or network). */
    fetchedAt: Ref<number | undefined>
    /** Triggers a background refresh. Resolves when done (success or failure). */
    refresh: () => Promise<void>
}

interface StoredPayload<T> {
    data: T
    fetchedAt: number
}

/**
 * Stale-while-revalidate for a single async resource.
 *
 * On creation it synchronously hydrates `data` from localStorage (if present),
 * so the UI can render last-known-good data immediately. Call `refresh()` to
 * fetch fresh data in the background; on success the cache is updated and the
 * reactive `data` swaps in (which lets consumers animate the change). On
 * failure the stale data is retained and `error` is populated.
 */
export function useCachedResource<T>(
    key: string,
    fetcher: () => Promise<T>
): CachedResource<T> {
    const storage = useLocalStorage()
    const data = ref<T | undefined>() as Ref<T | undefined>
    const loading = ref(true)
    const refreshing = ref(false)
    const error = ref<unknown>()
    const fetchedAt = ref<number | undefined>()

    const readCache = (): StoredPayload<T> | null =>
        storage.getItem<StoredPayload<T>>(key)
    const writeCache = (value: T, at: number): void =>
        storage.setItem<StoredPayload<T>>(key, { data: value, fetchedAt: at })

    // Synchronous hydration: render cached data before any network call.
    const cached = readCache()
    if (cached) {
        data.value = cached.data
        fetchedAt.value = cached.fetchedAt
        loading.value = false
    }

    const refresh = async (): Promise<void> => {
        refreshing.value = true
        try {
            const value = await fetcher()
            const at = Date.now()
            data.value = value
            fetchedAt.value = at
            error.value = undefined
            writeCache(value, at)
            loading.value = false
        } catch (err) {
            // Keep showing stale data; only surface a hard loading state when
            // we genuinely have nothing to render.
            error.value = err
            loading.value = data.value === undefined
        } finally {
            refreshing.value = false
        }
    }

    return { data, loading, refreshing, error, fetchedAt, refresh }
}
