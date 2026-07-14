// noinspection JSUnusedGlobalSymbols,JSUnresolvedReference

const editorInstances = new Map();
const WAIT_FOR_QUILL_MS = 1500;
const TOOLBAR_OPTIONS = [
    [{header: [1, 2, 3, false]}],
    ["bold", "italic", "underline", "strike"],
    ["blockquote", "code-block"],
    [{list: "ordered"}, {list: "bullet"}],
    ["link"],
    ["clean"]
];

function ensureQuillAvailable() {
    if (typeof window.Quill !== "undefined") {
        return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
        const start = Date.now();

        const timer = window.setInterval(() => {
            if (typeof window.Quill !== "undefined") {
                window.clearInterval(timer);
                resolve();
                return;
            }

            if (Date.now() - start > WAIT_FOR_QUILL_MS) {
                window.clearInterval(timer);
                reject(new Error("Quill library did not load in time."));
            }
        }, 25);
    });
}

function extractEditorHtml(quill) {
    if (typeof quill.getSemanticHTML === "function") {
        return quill.getSemanticHTML();
    }

    return quill.root.innerHTML;
}

export async function initialize(editorId, editorElement, initialHtml, dotNetReference) {
    await ensureQuillAvailable();

    const existing = editorInstances.get(editorId);
    if (existing) {
        dispose(editorId);
    }

    const quill = new window.Quill(editorElement, {
        theme: "snow",
        modules: {
            toolbar: TOOLBAR_OPTIONS
        }
    });

    if (initialHtml) {
        quill.clipboard.dangerouslyPasteHTML(initialHtml);
    }

    const state = {
        quill,
        timer: null,
        dotNetReference,
        handler: null,
        textChangeHandler: null
    };

    const handler = () => {
        if (state.timer) {
            window.clearTimeout(state.timer);
        }

        state.timer = window.setTimeout(async () => {
            const safeValue = extractEditorHtml(quill);
            try {
                await state.dotNetReference.invokeMethodAsync("NotifyValueChanged", safeValue);
            } catch {
                // ignore disconnected/teardown races
            }
        }, 250);
    };

    state.handler = handler;
    state.textChangeHandler = (delta, old, source) => {
        if (source !== "user") {
            return;
        }
        handler();
    };

    quill.on("text-change", state.textChangeHandler);

    editorInstances.set(editorId, state);
}

export function setContent(editorId, html) {
    const state = editorInstances.get(editorId);
    if (!state) {
        return;
    }

    if (state.textChangeHandler) {
        state.quill.off("text-change", state.textChangeHandler);
    }
    state.quill.setContents([], "silent");

    if (html) {
        state.quill.clipboard.dangerouslyPasteHTML(html, "silent");
    }

    if (state.textChangeHandler) {
        state.quill.on("text-change", state.textChangeHandler);
    }
}

export function dispose(editorId) {
    const state = editorInstances.get(editorId);
    if (!state) {
        return;
    }

    if (state.timer) {
        window.clearTimeout(state.timer);
    }

    if (state.textChangeHandler) {
        state.quill.off("text-change", state.textChangeHandler);
    }

    state.quill.off("selection-change");
    state.quill.off("editor-change");
    editorInstances.delete(editorId);
}
