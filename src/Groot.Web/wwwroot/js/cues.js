// Voice + beeps for the interval runner: platform speech synthesis, WebAudio for the tones.
// Nothing leaves the device and nothing is downloaded — the PWA keeps working offline.

let audio;

function context() {
    audio ??= new (window.AudioContext || window.webkitAudioContext)();
    if (audio.state === 'suspended') {
        audio.resume();
    }
    return audio;
}

const TONES = {
    // [frequency, duration seconds] pairs, played back to back
    RunStart: [[880, 0.12], [1320, 0.16]],
    WalkStart: [[440, 0.22]],
    Warning: [[660, 0.08]],
    Finish: [[660, 0.12], [880, 0.12], [1320, 0.26]],
};

export function beep(sound) {
    const tones = TONES[sound] ?? TONES.Warning;
    const ctx = context();
    let at = ctx.currentTime;

    for (const [frequency, duration] of tones) {
        const oscillator = ctx.createOscillator();
        const gain = ctx.createGain();

        oscillator.type = 'sine';
        oscillator.frequency.setValueAtTime(frequency, at);
        gain.gain.setValueAtTime(0.0001, at);
        gain.gain.exponentialRampToValueAtTime(0.28, at + 0.015);
        gain.gain.exponentialRampToValueAtTime(0.0001, at + duration);

        oscillator.connect(gain).connect(ctx.destination);
        oscillator.start(at);
        oscillator.stop(at + duration + 0.02);
        at += duration + 0.03;
    }
}

export function speak(text, language) {
    if (!('speechSynthesis' in window)) {
        return;
    }
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = language;
    utterance.rate = 1.0;
    // the beep runs ~300ms ahead so the voice lands after the tone, as on the phone heads
    window.setTimeout(() => window.speechSynthesis.speak(utterance), 300);
}

export function silence() {
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
    }
}
