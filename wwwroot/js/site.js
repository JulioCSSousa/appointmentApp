window.AppointmentNotifications = {
    startWorkerAlerts: function () {
        if (window.__appointmentNotificationsStarted) {
            return;
        }

        window.__appointmentNotificationsStarted = true;

        const enableButton = document.getElementById("enableSoundAlerts");
        let audioContext;

        function getAudioContext() {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) {
                throw new Error("Este navegador não oferece suporte a áudio.");
            }

            audioContext ??= new AudioContext();
            return audioContext;
        }

        async function enableSound() {
            const context = getAudioContext();
            await context.resume();
            localStorage.setItem("appointmentSoundEnabled", "true");
            playAlertSound();

            if (enableButton) {
                enableButton.textContent = "Som ativado";
                enableButton.classList.remove("btn-outline-primary");
                enableButton.classList.add("btn-success");
            }
        }

        function playAlertSound() {
            const context = getAudioContext();
            if (context.state !== "running") {
                return;
            }

            const oscillator = context.createOscillator();
            const gain = context.createGain();
            const now = context.currentTime;

            oscillator.type = "sine";
            oscillator.frequency.setValueAtTime(880, now);
            oscillator.frequency.setValueAtTime(660, now + 0.18);
            gain.gain.setValueAtTime(0.0001, now);
            gain.gain.exponentialRampToValueAtTime(0.25, now + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.45);

            oscillator.connect(gain);
            gain.connect(context.destination);
            oscillator.start(now);
            oscillator.stop(now + 0.45);
        }

        if (enableButton) {
            enableButton.addEventListener("click", function () {
                enableSound().catch(function (error) {
                    console.error("Não foi possível ativar o som.", error);
                    enableButton.textContent = "Não foi possível ativar o som";
                });
            });
            if (localStorage.getItem("appointmentSoundEnabled") === "true") {
                enableSound().catch(() => { });
            }
        }

        if (!window.signalR) {
            console.error("A biblioteca SignalR não foi carregada.");
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNewAppointment", function (message) {
            playAlertSound();

            const alert = document.getElementById("appointmentNotification");
            if (alert) {
                alert.textContent = message || "Novo agendamento recebido!";
                alert.classList.remove("d-none");
            }

            setTimeout(function () {
                window.location.reload();
            }, 1000);
        });

        connection.start().catch(function (error) {
            console.error("Não foi possível conectar às notificações de agendamento.", error);
        });
    }
};
