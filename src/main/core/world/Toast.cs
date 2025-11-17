using Godot;
using System;

public partial class Toast {
    private LIFETIME life = 0;
    private int lifeCustomTime;
    private ToastManager toastManager = null;

    public enum LIFETIME {
        INFINITE = -25,
        HALF_A_SECOND = 500,
        SECOND = 1000,
        TWO_SECONDS = 2000,
    };

    public Toast(ToastManager toastManager, LIFETIME life) {
        this.toastManager = toastManager;
        this.life = life;
    }

    public Toast(ToastManager toastManager, int life) {
        this.toastManager = toastManager;
        lifeCustomTime = life;
    }

    public void post(string text) {
        toastManager.Visible = true;
        toastManager.textLabel.Text = text;
        toastManager.lifeLeft = (life == 0)
            ? lifeCustomTime
            : (int)life;
    }

    public void hide() {
        toastManager.Visible = false;
    }
}