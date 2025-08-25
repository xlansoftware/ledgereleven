// lib/playSound.ts
let audioCtx: AudioContext | null = null;

export async function playSound(url: string, volume: number = 0.5) {
  try {
    if (!audioCtx) {
      // Create a single AudioContext (recommended instead of making a new one each time)
      audioCtx = new AudioContext();
    }

    // Fetch audio data
    const response = await fetch(url);
    const arrayBuffer = await response.arrayBuffer();

    // Decode audio into a buffer
    const audioBuffer = await audioCtx.decodeAudioData(arrayBuffer);

    // Create a buffer source
    const source = audioCtx.createBufferSource();
    source.buffer = audioBuffer;

    // Create a gain node for volume control
    const gainNode = audioCtx.createGain();
    gainNode.gain.value = volume; // 0.0 = silent, 1.0 = full volume

    // Connect nodes: source → gain → destination (speakers)
    source.connect(gainNode);
    gainNode.connect(audioCtx.destination);

    // Start playback immediately
    source.start();
  } catch (e) {
    console.warn("Playback failed:", e);
  }
}
