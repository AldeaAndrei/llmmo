import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/context/AuthContext'
import { findFreeTile } from '@/lib/map'
import { api } from '@/lib/api'

function RegisterForm() {
  const { register } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [playerName, setPlayerName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setError('')

    try {
      const map = await api.getMap()
      const { x, y } = findFreeTile(map)
      await register({ email: email.trim(), password, playerName: playerName.trim(), x, y })
    } catch (err) {
      setError(err.message ?? 'Registration failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <div className="space-y-1">
        <label htmlFor="register-email" className="text-xs font-medium">
          Email
        </label>
        <input
          id="register-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        />
      </div>
      <div className="space-y-1">
        <label htmlFor="register-password" className="text-xs font-medium">
          Password
        </label>
        <input
          id="register-password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
          className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        />
      </div>
      <div className="space-y-1">
        <label htmlFor="register-name" className="text-xs font-medium">
          Player name
        </label>
        <input
          id="register-name"
          type="text"
          value={playerName}
          onChange={(e) => setPlayerName(e.target.value)}
          required
          maxLength={30}
          className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        />
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <Button type="submit" className="w-full" disabled={submitting || !playerName.trim()}>
        {submitting ? 'Creating account…' : 'Create account'}
      </Button>
    </form>
  )
}

export default RegisterForm
