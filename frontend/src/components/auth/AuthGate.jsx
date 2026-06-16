import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/context/AuthContext'
import LoginForm from '@/components/auth/LoginForm'
import RegisterForm from '@/components/auth/RegisterForm'
import SpectateShell from '@/components/llm/SpectateShell'

function AuthGate({ children }) {
  const { isAuthenticated, loading } = useAuth()
  const [mode, setMode] = useState('login')
  const [spectate, setSpectate] = useState(
    () => window.location.hash === '#watch',
  )

  useEffect(() => {
    function syncHash() {
      setSpectate(window.location.hash === '#watch')
    }

    window.addEventListener('hashchange', syncHash)
    return () => window.removeEventListener('hashchange', syncHash)
  }, [])

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Loading…
      </div>
    )
  }

  if (!isAuthenticated && spectate) {
    return (
      <SpectateShell
        onSignIn={() => {
          window.location.hash = ''
          setSpectate(false)
        }}
      />
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="flex h-full flex-col items-center justify-center p-6">
        <div className="w-full max-w-md space-y-6">
          <div className="text-center">
            <h1 className="text-2xl font-semibold">LLMMO</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Sign in or create an account to play
            </p>
          </div>

          <div className="flex gap-2">
            <Button
              type="button"
              variant={mode === 'login' ? 'default' : 'outline'}
              className="flex-1"
              onClick={() => setMode('login')}
            >
              Log in
            </Button>
            <Button
              type="button"
              variant={mode === 'register' ? 'default' : 'outline'}
              className="flex-1"
              onClick={() => setMode('register')}
            >
              Register
            </Button>
          </div>

          {mode === 'login' ? <LoginForm /> : <RegisterForm />}

          <div className="text-center">
            <Button
              type="button"
              variant="link"
              className="text-sm"
              onClick={() => {
                window.location.hash = '#watch'
                setSpectate(true)
              }}
            >
              Watch LLM agents without signing in
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return children
}

export default AuthGate
