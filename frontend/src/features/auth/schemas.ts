import { z } from 'zod'

// Mirrors the server rules in specs/001-user-authentication/data-model.md; the server stays the
// source of truth and its field errors are shown the same way.
const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/
const PASSWORD_LENGTH_MESSAGE = 'Password must be between 8 and 128 characters.'

export const registerSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(1, 'Display name is required.')
    .max(100, 'Display name must be at most 100 characters.'),
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(254, 'Email must be at most 254 characters.')
    .regex(EMAIL_PATTERN, 'Enter a valid email address.'),
  password: z
    .string()
    .min(8, PASSWORD_LENGTH_MESSAGE)
    .max(128, PASSWORD_LENGTH_MESSAGE)
    .refine((password) => password.trim().length > 0, 'Password cannot be only spaces.'),
})

export type RegisterValues = z.infer<typeof registerSchema>

export const signInSchema = z.object({
  email: z.string().trim().min(1, 'Email is required.'),
  password: z.string().min(1, 'Password is required.'),
})

export type SignInValues = z.infer<typeof signInSchema>
