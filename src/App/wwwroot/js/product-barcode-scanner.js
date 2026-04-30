(function () {
    const scanner = document.querySelector("[data-barcode-scanner]");
    if (!scanner) {
        return;
    }

    const input = scanner.querySelector("[data-barcode-input]");
    const startButton = scanner.querySelector("[data-barcode-start]");
    const stopButton = scanner.querySelector("[data-barcode-stop]");
    const preview = scanner.querySelector("[data-barcode-preview]");
    const readerElement = scanner.querySelector("[data-barcode-reader]");
    const status = scanner.querySelector("[data-barcode-status]");
    const form = scanner.closest("form");

    let html5QrCode = null;
    let isScanning = false;

    function setStatus(message, kind) {
        if (status) {
            status.textContent = message;
            status.classList.remove("text-muted", "text-success", "text-danger");
            status.classList.add(kind === "success" ? "text-success" : kind === "error" ? "text-danger" : "text-muted");
        }
    }

    function errorDetail(error) {
        if (!error) {
            return "Detalhe: erro desconhecido.";
        }

        const name = error.name || "Erro desconhecido";
        const message = error.message ? " - " + error.message : "";
        return "Detalhe: " + name + message + ".";
    }

    function barcodeErrorMessage(error) {
        if (!error) {
            return "Erro desconhecido ao acessar a camera. Informe o codigo manualmente. Detalhe: erro desconhecido.";
        }

        if (error.name === "NotAllowedError" || error.name === "SecurityError" || error.name === "PermissionDeniedError") {
            return "Permissao da camera negada. Informe o codigo manualmente. " + errorDetail(error);
        }

        if (error.name === "NotFoundError" || error.name === "DevicesNotFoundError") {
            return "Camera nao encontrada neste dispositivo. Informe o codigo manualmente. " + errorDetail(error);
        }

        if (error.name === "NotReadableError" || error.name === "TrackStartError" || error.name === "AbortError") {
            return "Camera ocupada ou indisponivel. Feche outros aplicativos e tente novamente, ou informe o codigo manualmente. " + errorDetail(error);
        }

        if (error.name === "OverconstrainedError" || error.name === "ConstraintNotSatisfiedError") {
            return "Nao foi possivel usar a camera traseira. Tente novamente ou informe o codigo manualmente. " + errorDetail(error);
        }

        return "Erro desconhecido ao acessar a camera. Informe o codigo manualmente. " + errorDetail(error);
    }

    async function failScan(message) {
        await stopScan();
        setStatus(message, "error");
    }

    function showPreview(show) {
        if (!preview) {
            return;
        }

        preview.classList.toggle("d-none", !show);
    }

    async function stopScan() {
        if (html5QrCode) {
            try {
                if (isScanning) {
                    await html5QrCode.stop();
                }

                await html5QrCode.clear();
            } catch (error) {
                try {
                    await html5QrCode.clear();
                } catch (clearError) {
                    // Stopping is best-effort because the UI must remain usable for manual entry.
                }
            }
        }

        isScanning = false;
        showPreview(false);
    }

    async function stopActiveScan() {
        if (html5QrCode && isScanning) {
            try {
                await html5QrCode.stop();
                await html5QrCode.clear();
            } catch (error) {
                // Stopping is best-effort because the UI must remain usable for manual entry.
            }
        }

        isScanning = false;
        showPreview(false);
    }

    async function startScan() {
        if (!input || !readerElement) {
            return;
        }

        if (!window.Html5Qrcode) {
            await failScan("Leitor de codigo de barras indisponivel. Informe o codigo manualmente. Detalhe: biblioteca html5-qrcode nao carregada.");
            return;
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            await failScan("Camera indisponivel neste navegador. Informe o codigo manualmente. Detalhe: mediaDevices/getUserMedia indisponivel.");
            return;
        }

        if (!window.isSecureContext) {
            await failScan("Camera bloqueada porque a pagina nao esta em contexto seguro. Acesse por HTTPS ou informe o codigo manualmente. Detalhe: contexto inseguro.");
            return;
        }

        await stopScan();
        showPreview(true);
        setStatus("Aponte a camera para o codigo de barras.", "neutral");

        try {
            const formats = window.Html5QrcodeSupportedFormats
                ? [
                    window.Html5QrcodeSupportedFormats.EAN_13,
                    window.Html5QrcodeSupportedFormats.EAN_8,
                    window.Html5QrcodeSupportedFormats.UPC_A,
                    window.Html5QrcodeSupportedFormats.UPC_E,
                    window.Html5QrcodeSupportedFormats.CODE_128,
                    window.Html5QrcodeSupportedFormats.CODE_39,
                    window.Html5QrcodeSupportedFormats.ITF
                ]
                : undefined;

            html5QrCode = new window.Html5Qrcode(readerElement.id, { formatsToSupport: formats });
            await html5QrCode.start(
                { facingMode: "environment" },
                { fps: 10, qrbox: { width: 250, height: 150 } },
                async function (decodedText) {
                    input.value = decodedText;
                    input.dispatchEvent(new Event("input", { bubbles: true }));
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                    setStatus("Codigo de barras lido.", "success");
                    await stopScan();
                });
            isScanning = true;
        } catch (error) {
            await failScan(barcodeErrorMessage(error));
        }
    }

    if (startButton) {
        startButton.addEventListener("click", startScan);
    }

    if (stopButton) {
        stopButton.addEventListener("click", async function () {
            await stopScan();
            setStatus("Leitura cancelada. Informe manualmente ou tente novamente.", "neutral");
        });
    }

    if (form) {
        form.addEventListener("submit", stopActiveScan);
    }

    window.addEventListener("pagehide", stopActiveScan);
})();
