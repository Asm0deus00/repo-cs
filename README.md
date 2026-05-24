# Racing Game — Proyecto Final Realidad Mixta

Juego de carreras físico-digital que integra un controlador Arduino con una experiencia en Unity. El jugador usa un volante físico (potenciómetros + botones) para controlar el coche en una pista, con un display 7 segmentos que muestra la velocidad en tiempo real.

---

## Hardware requerido

| Componente | Valor / Modelo | Función |
|------------|---------------|---------|
| Arduino Uno / Nano | — | Microcontrolador principal |
| Potenciómetro 1 | 10kΩ | Dirección (steering) |
| Potenciómetro 2 | 5kΩ | Velocidad |
| Potenciómetro 3 | 1kΩ | Navegación menú pausa |
| Botón A | — | Turbo (tap) |
| Botón B | — | Pausa / confirmar |
| Display 7-seg | TM1637 | Muestra velocidad actual |
| Resistencias | 10kΩ × 2 | Pull-down para botones |

---

## Cableado

```
Arduino A0 → Pot 10kΩ (steering)     — pin central del pot
Arduino A1 → Pot  5kΩ (speed)        — pin central del pot
Arduino A2 → Pot  1kΩ (menu nav)     — pin central del pot

Todos los pots: extremos a 5V y GND respectivamente.

Arduino D2 → Botón turbo → GND  + resistencia 10kΩ a GND (pull-down)
Arduino D3 → Botón pausa → GND  + resistencia 10kΩ a GND (pull-down)

Arduino D5 (CLK) → TM1637 CLK
Arduino D6 (DIO) → TM1637 DIO
Arduino 5V       → TM1637 VCC
Arduino GND      → TM1637 GND
```

---

## Cómo cargar el sketch en Arduino

1. Abrir Arduino IDE.
2. Instalar librería `TM1637Display` by *Avishay Orpaz* (Tools → Manage Libraries).
3. Abrir `Arduino/racing_controller/racing_controller.ino`.
4. Seleccionar la placa correcta y el puerto COM.
5. Upload.

---

## Cómo abrir el proyecto Unity

1. Tener Unity **2022.3 LTS** instalado.
2. Abrir Unity Hub → Add → seleccionar la carpeta `Unity/`.
3. El proyecto usa el paquete GLTFUtility o el importer nativo de Unity 2022 para los modelos `.glb`.
4. Asegurarse de tener el package **TextMeshPro** instalado (normalmente incluido).

### Estructura de escenas (Build Settings)

| Índice | Escena | Descripción |
|--------|--------|-------------|
| 0 | `PortSelect` | Selector de puerto COM |
| 1 | `Race` | Carrera principal |
| 2 | `EndScreen` | Resultados finales |

---

## Cómo ejecutar el build

1. Conectar el Arduino por USB.
2. Ejecutar `Build/RacingGame.exe`.
3. En la pantalla de inicio, seleccionar el puerto COM del Arduino.
4. Click en **Conectar** → comienza la carrera.

---

## Controles

| Control físico | Acción |
|----------------|--------|
| Pot 1 (10kΩ) | Dirección izquierda / derecha |
| Pot 2 (5kΩ) | Velocidad (0 = parado, máximo = velocidad tope) |
| Botón A (turbo) | Tap → burst de velocidad (recarga ~3s) |
| Botón B (pausa) | Tap → abre pausa / confirma selección |
| Pot 3 (1kΩ) | Solo en pausa: selecciona Resume o Restart |

---

## Objetivo del juego

Completar 3 vueltas al circuito en el menor tiempo posible. La pantalla final muestra el tiempo de cada vuelta, el total y la mejor vuelta.

---

## Protocolo Serial

**Baud rate:** 115200

- **Arduino → Unity (cada ~20ms):** `pot1,pot2,pot3,turboTap,pauseTap\n`
- **Unity → Arduino (cada ~100ms):** `speed\n` (entero 0–999 en km/h)
