#include <TM1637Display.h>

// ── Pines ──────────────────────────────────────────────
#define PIN_POT_STEER   A0
#define PIN_POT_SPEED   A1
#define PIN_POT_MENU    A2
#define PIN_BTN_TURBO   2
#define PIN_BTN_PAUSE   3
#define CLK_DISPLAY     5
#define DIO_DISPLAY     6

// ── Display ────────────────────────────────────────────
TM1637Display display(CLK_DISPLAY, DIO_DISPLAY);

// ── Estado botones (detección de tap) ──────────────────
bool lastTurbo = false;
bool lastPause = false;

// ── Envío Serial ───────────────────────────────────────
unsigned long lastSendTime = 0;
const unsigned long SEND_INTERVAL = 20;       // ms

// ── Recepción Serial ───────────────────────────────────
String serialBuffer = "";
unsigned long lastDisplayUpdate = 0;
const unsigned long DISPLAY_INTERVAL = 100;   // ms
int displaySpeed = 0;

// ── Setup ──────────────────────────────────────────────
void setup() {
  Serial.begin(115200);

  pinMode(PIN_BTN_TURBO, INPUT);
  pinMode(PIN_BTN_PAUSE, INPUT);

  display.setBrightness(7);
  display.showNumberDec(0, true);
}

// ── Loop ───────────────────────────────────────────────
void loop() {
  unsigned long now = millis();

  // — Leer entradas —
  int pot1 = analogRead(PIN_POT_STEER);
  int pot2 = analogRead(PIN_POT_SPEED);
  int pot3 = analogRead(PIN_POT_MENU);

  bool turboNow = digitalRead(PIN_BTN_TURBO) == HIGH;
  bool pauseNow = digitalRead(PIN_BTN_PAUSE) == HIGH;

  // Detectar tap (flanco de subida)
  int turboTap = (!lastTurbo && turboNow) ? 1 : 0;
  int pauseTap = (!lastPause && pauseNow) ? 1 : 0;

  lastTurbo = turboNow;
  lastPause = pauseNow;

  // — Enviar datos a Unity cada SEND_INTERVAL ms —
  if (now - lastSendTime >= SEND_INTERVAL) {
    lastSendTime = now;
    Serial.print(pot1);     Serial.print(',');
    Serial.print(pot2);     Serial.print(',');
    Serial.print(pot3);     Serial.print(',');
    Serial.print(turboTap); Serial.print(',');
    Serial.println(pauseTap);
  }

  // — Recibir velocidad desde Unity —
  while (Serial.available() > 0) {
    char c = Serial.read();
    if (c == '\n') {
      int speed = serialBuffer.toInt();
      speed = max(0, min(999, speed));   // clamp 0–999, nunca negativo
      displaySpeed = speed;
      serialBuffer = "";
    } else {
      serialBuffer += c;
    }
  }

  // — Actualizar display cada DISPLAY_INTERVAL ms —
  if (now - lastDisplayUpdate >= DISPLAY_INTERVAL) {
    lastDisplayUpdate = now;
    display.showNumberDec(displaySpeed, false);
  }
}
