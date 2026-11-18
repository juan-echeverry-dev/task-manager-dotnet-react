import { useQuery } from '@tanstack/react-query'
import { useSession } from '../auth/session/useSession'
import { getProfile } from './profile-api'

const memberSinceFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'long' })

/** Profile of the signed-in person (spec FR-011). Rendered only inside RequireAuth. */
export function ProfilePage() {
  const { session } = useSession()
  const accessToken = session?.accessToken ?? ''
  const profile = useQuery({
    queryKey: ['profile', accessToken],
    queryFn: () => getProfile(accessToken),
    enabled: accessToken !== '',
  })

  return (
    <section className="mx-auto w-full max-w-md space-y-6">
      <h1 className="text-2xl font-semibold">Your profile</h1>

      {profile.isPending ? <p className="text-slate-600">Loading your profile…</p> : null}

      {profile.isError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          We couldn't load your profile. Please try again.
        </p>
      ) : null}

      {profile.data ? (
        <dl className="divide-y divide-slate-200 rounded-md border border-slate-200 bg-white">
          <ProfileRow label="Display name" value={profile.data.displayName} />
          <ProfileRow label="Email" value={profile.data.email} />
          <ProfileRow label="Member since" value={memberSinceFormat.format(new Date(profile.data.createdAt))} />
        </dl>
      ) : null}
    </section>
  )
}

function ProfileRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1 px-4 py-3 sm:flex-row sm:gap-4">
      <dt className="text-sm font-medium text-slate-500 sm:w-36">{label}</dt>
      <dd className="break-all text-slate-900">{value}</dd>
    </div>
  )
}
