-- =============================================================
--  Billetera Digital — Esquema PostgreSQL
--  Curso: Diseño y Arquitectura de Software
-- =============================================================
-- Ejecutar conectado a la base "billetera_digital".
-- (docker compose lo ejecuta automáticamente la primera vez)

CREATE EXTENSION IF NOT EXISTS pgcrypto;   -- gen_random_uuid() para los datos semilla

-- ---------------------------------------------------- CLIENTES
CREATE TABLE IF NOT EXISTS clientes (
    id              UUID         PRIMARY KEY,
    nombres         VARCHAR(80)  NOT NULL,
    apellidos       VARCHAR(80)  NOT NULL,
    dni             CHAR(8)      NOT NULL UNIQUE CHECK (dni ~ '^[0-9]{8}$'),
    email           VARCHAR(120) NOT NULL UNIQUE,
    telefono        VARCHAR(15)  NOT NULL DEFAULT '',
    password_hash   TEXT         NOT NULL,
    fecha_registro  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

-- ---------------------------------------------------- BILLETERAS
-- Relación 1 a 1 con clientes (cliente_id UNIQUE).
CREATE TABLE IF NOT EXISTS billeteras (
    id              UUID          PRIMARY KEY,
    numero_cuenta   CHAR(10)      NOT NULL UNIQUE CHECK (numero_cuenta ~ '^[0-9]{10}$'),
    cliente_id      UUID          NOT NULL UNIQUE REFERENCES clientes(id) ON DELETE RESTRICT,
    saldo           NUMERIC(14,2) NOT NULL DEFAULT 0 CHECK (saldo >= 0),   -- última línea de defensa
    moneda          CHAR(3)       NOT NULL DEFAULT 'PEN',
    fecha_creacion  TIMESTAMPTZ   NOT NULL DEFAULT now()
);

-- ---------------------------------------------------- TRANSACCIONES
-- Una transferencia genera 2 filas (Enviada / Recibida) con la misma "referencia".
CREATE TABLE IF NOT EXISTS transacciones (
    id                        UUID          PRIMARY KEY,
    billetera_id              UUID          NOT NULL REFERENCES billeteras(id),
    tipo                      VARCHAR(30)   NOT NULL
                              CHECK (tipo IN ('Recarga','TransferenciaEnviada','TransferenciaRecibida')),
    monto                     NUMERIC(14,2) NOT NULL CHECK (monto > 0),
    comision                  NUMERIC(14,2) NOT NULL DEFAULT 0 CHECK (comision >= 0),
    saldo_resultante          NUMERIC(14,2) NOT NULL,
    billetera_contraparte_id  UUID          NULL REFERENCES billeteras(id),
    descripcion               VARCHAR(140)  NOT NULL DEFAULT '',
    referencia                UUID          NOT NULL,
    fecha                     TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT ck_contraparte CHECK (
        (tipo = 'Recarga' AND billetera_contraparte_id IS NULL) OR
        (tipo <> 'Recarga' AND billetera_contraparte_id IS NOT NULL)
    )
);

CREATE INDEX IF NOT EXISTS ix_transacciones_billetera_fecha
    ON transacciones (billetera_id, fecha DESC);
CREATE INDEX IF NOT EXISTS ix_transacciones_referencia
    ON transacciones (referencia);

-- ---------------------------------------------------- VISTA DE APOYO (consultas en clase)
CREATE OR REPLACE VIEW v_historial AS
SELECT t.fecha,
       b.numero_cuenta,
       c.nombres || ' ' || c.apellidos AS titular,
       t.tipo,
       t.monto,
       t.comision,
       t.saldo_resultante,
       bc.numero_cuenta                AS cuenta_contraparte,
       t.descripcion,
       t.referencia
FROM transacciones t
JOIN billeteras b       ON b.id = t.billetera_id
JOIN clientes   c       ON c.id = b.cliente_id
LEFT JOIN billeteras bc ON bc.id = t.billetera_contraparte_id;
