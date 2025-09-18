const audio = new Audio("/sounds/success.mp3");

export function playSound() {
   audio.play().catch((e) => console.warn("Playback blocked:", e));
}
