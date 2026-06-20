import { useState } from 'react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useAuth } from '@/context/AuthContext'
import { useSocial } from '@/context/SocialContext'
import { useTickTime } from '@/hooks/useTickTime'

const fieldClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50'

function relationLabel(relation) {
  if (relation === 'ally') return 'Ally'
  if (relation === 'enemy') return 'Enemy'
  return relation
}

function formatTime(sentAt) {
  if (!sentAt) return ''
  return new Date(sentAt).toLocaleString()
}

function CooldownBanner({ cooldowns, formatRemainingTicks }) {
  if (!cooldowns) return null

  const messageRemaining = cooldowns.message?.remainingTicks ?? 0
  const diplomacyRemaining = cooldowns.diplomacy?.remainingTicks ?? 0

  if (messageRemaining === 0 && diplomacyRemaining === 0) {
    return null
  }

  return (
    <div className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm">
      {messageRemaining > 0 && (
        <p>Message available in {formatRemainingTicks(messageRemaining)}</p>
      )}
      {diplomacyRemaining > 0 && (
        <p>
          Diplomacy available in {formatRemainingTicks(diplomacyRemaining)}
        </p>
      )}
    </div>
  )
}

function SocialPanel() {
  const { isAuthenticated } = useAuth()
  const {
    players,
    messages,
    relations,
    cooldowns,
    loading,
    hasLoaded,
    sendMessage,
    markMessageRead,
    setRelation,
    clearRelation,
  } = useSocial()
  const { formatRemainingTicks } = useTickTime()

  const [targetPlayerId, setTargetPlayerId] = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [expandedMessageId, setExpandedMessageId] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const [diplomacyBusy, setDiplomacyBusy] = useState(false)

  const messageCooldown = cooldowns?.message?.remainingTicks ?? 0
  const diplomacyCooldown = cooldowns?.diplomacy?.remainingTicks ?? 0
  const canSendMessage = messageCooldown === 0 && targetPlayerId && subject.trim() && body.trim()
  const canDeclare = diplomacyCooldown === 0 && targetPlayerId

  const handleSend = async (event) => {
    event.preventDefault()
    if (!canSendMessage) return

    setSubmitting(true)
    try {
      await sendMessage(targetPlayerId, subject.trim(), body.trim())
      setSubject('')
      setBody('')
      toast.success('Message sent')
    } catch (err) {
      toast.error(err.message ?? 'Failed to send message')
    } finally {
      setSubmitting(false)
    }
  }

  const handleRelation = async (relation) => {
    if (!canDeclare) return

    setDiplomacyBusy(true)
    try {
      await setRelation(targetPlayerId, relation)
      toast.success(relation === 'ally' ? 'Alliance declared' : 'War declared')
    } catch (err) {
      toast.error(err.message ?? 'Failed to update relation')
    } finally {
      setDiplomacyBusy(false)
    }
  }

  const handleClearRelation = async (toPlayerId) => {
    if (diplomacyCooldown > 0) return

    setDiplomacyBusy(true)
    try {
      await clearRelation(toPlayerId)
      toast.success('Relation cleared')
    } catch (err) {
      toast.error(err.message ?? 'Failed to clear relation')
    } finally {
      setDiplomacyBusy(false)
    }
  }

  const toggleMessage = async (message) => {
    const isExpanded = expandedMessageId === message.id
    setExpandedMessageId(isExpanded ? null : message.id)

    if (!isExpanded && !message.readAt) {
      await markMessageRead(message.id)
    }
  }

  if (!isAuthenticated) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Log in to use diplomacy
      </div>
    )
  }

  if (loading && !hasLoaded) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Loading diplomacy…
      </div>
    )
  }

  return (
    <div className="h-full overflow-auto p-4">
      <div className="mx-auto max-w-3xl space-y-6">
        <div>
          <h2 className="font-medium">Social</h2>
          <p className="text-sm text-muted-foreground">
            Send messages and declare alliances or war
          </p>
        </div>

        <CooldownBanner
          cooldowns={cooldowns}
          formatRemainingTicks={formatRemainingTicks}
        />

        <form onSubmit={handleSend} className="space-y-3 rounded-lg border p-4">
          <h3 className="text-sm font-medium">Compose</h3>

          <div className="space-y-1">
            <label htmlFor="social-target" className="text-xs font-medium">
              Player
            </label>
            <select
              id="social-target"
              value={targetPlayerId}
              onChange={(event) => setTargetPlayerId(event.target.value)}
              className={cn(fieldClassName, 'h-9')}
            >
              <option value="">Select a player…</option>
              {players.map((player) => (
                <option key={player.id} value={player.id}>
                  {player.name} ({player.playerType})
                </option>
              ))}
            </select>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={!canDeclare || diplomacyBusy}
              onClick={() => handleRelation('ally')}
            >
              Ally
            </Button>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={!canDeclare || diplomacyBusy}
              onClick={() => handleRelation('enemy')}
            >
              Declare War
            </Button>
          </div>

          <div className="space-y-1">
            <label htmlFor="social-subject" className="text-xs font-medium">
              Subject
            </label>
            <input
              id="social-subject"
              value={subject}
              onChange={(event) => setSubject(event.target.value)}
              maxLength={100}
              className={cn(fieldClassName, 'h-9')}
              placeholder="Subject"
            />
          </div>

          <div className="space-y-1">
            <label htmlFor="social-body" className="text-xs font-medium">
              Message
            </label>
            <textarea
              id="social-body"
              value={body}
              onChange={(event) => setBody(event.target.value)}
              maxLength={500}
              rows={4}
              className={fieldClassName}
              placeholder="Up to 500 characters"
            />
            <p className="text-xs text-muted-foreground">{body.length}/500</p>
          </div>

          <Button type="submit" disabled={!canSendMessage || submitting}>
            Send message
          </Button>
        </form>

        <section className="space-y-3 rounded-lg border p-4">
          <h3 className="text-sm font-medium">Relations</h3>
          {relations.length === 0 ? (
            <p className="text-sm text-muted-foreground">No active alliances or wars.</p>
          ) : (
            <ul className="space-y-2">
              {relations.map((relation) => (
                <li
                  key={relation.otherPlayerId}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border bg-muted/30 px-3 py-2 text-sm"
                >
                  <div>
                    <span className="font-medium">{relation.otherPlayerName}</span>
                    <span className="ml-2 text-xs capitalize text-muted-foreground">
                      {relation.otherPlayerType}
                    </span>
                    <span
                      className={cn(
                        'ml-2 rounded px-1.5 py-0.5 text-xs font-medium',
                        relation.relation === 'ally'
                          ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-400'
                          : 'bg-destructive/15 text-destructive',
                      )}
                    >
                      {relationLabel(relation.relation)}
                    </span>
                  </div>
                  <Button
                    type="button"
                    size="sm"
                    variant="ghost"
                    disabled={diplomacyCooldown > 0 || diplomacyBusy}
                    onClick={() => handleClearRelation(relation.otherPlayerId)}
                  >
                    Clear
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="space-y-3 rounded-lg border p-4">
          <h3 className="text-sm font-medium">Messages</h3>
          {messages.length === 0 ? (
            <p className="text-sm text-muted-foreground">No messages yet.</p>
          ) : (
            <ul className="space-y-2">
              {messages.map((message) => {
                const isExpanded = expandedMessageId === message.id
                const isUnread = !message.readAt

                return (
                  <li key={message.id}>
                    <button
                      type="button"
                      onClick={() => toggleMessage(message)}
                      className={cn(
                        'flex w-full flex-col rounded-md border px-3 py-2 text-left text-sm transition-colors',
                        isExpanded
                          ? 'border-primary bg-primary/10'
                          : 'border-border bg-muted/30 hover:bg-muted/60',
                      )}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <span className="font-medium">{message.subject}</span>
                        {isUnread && (
                          <span className="shrink-0 rounded bg-primary/20 px-1.5 py-0.5 text-xs text-primary">
                            New
                          </span>
                        )}
                      </div>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {message.fromPlayerName} → {message.toPlayerName} ·{' '}
                        {formatTime(message.sentAt)}
                      </p>
                      {isExpanded && (
                        <p className="mt-2 whitespace-pre-wrap text-sm">{message.body}</p>
                      )}
                    </button>
                  </li>
                )
              })}
            </ul>
          )}
        </section>
      </div>
    </div>
  )
}

export default SocialPanel
