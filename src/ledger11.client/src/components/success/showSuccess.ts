import { showSuccessInternal } from "./SuccessOverlayProvider"

export async function showSuccess(): Promise<void> {
  return new Promise((resolve) => {
    showSuccessInternal(resolve)
  })
}
