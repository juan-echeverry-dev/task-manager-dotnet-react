import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

afterEach(() => {
  cleanup()
})

// jsdom does not implement <dialog>. This stand-in covers what the app uses: showModal/close toggle the
// `open` attribute, and Escape fires `cancel` as browsers do. Real focus trapping is the browser's job.
if (typeof HTMLDialogElement !== 'undefined' && !('showModal' in HTMLDialogElement.prototype)) {
  Object.assign(HTMLDialogElement.prototype, {
    showModal(this: HTMLDialogElement) {
      this.setAttribute('open', '')
    },
    close(this: HTMLDialogElement) {
      if (!this.hasAttribute('open')) return
      this.removeAttribute('open')
      this.dispatchEvent(new Event('close'))
    },
  })
  document.addEventListener('keydown', (event) => {
    const dialog = (event.target as Element | null)?.closest?.('dialog[open]')
    if (event.key === 'Escape' && dialog) {
      dialog.dispatchEvent(new Event('cancel', { cancelable: true }))
    }
  })
}
