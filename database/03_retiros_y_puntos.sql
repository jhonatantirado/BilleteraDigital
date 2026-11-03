-- =============================================================
--  Billetera Digital — Ejercicio 2: Retiros + Puntos de fidelidad
-- =============================================================
-- docker compose lo ejecuta automáticamente SOLO al crear la base por
-- primera vez. Si tu base ya existe, ejecútalo a mano:
--   docker exec -i billetera_postgres psql -U billetera -d billetera_digital < database/03_retiros_y_puntos.sql
-- Es idempotente: puede ejecutarse varias veces sin error.

-- ---------------------------------------------------- RETIROS
-- 1) Aceptar el nuevo tipo 'Retiro'
ALTER TABLE transacciones DROP CONSTRAINT IF EXISTS transacciones_tipo_check;
ALTER TABLE transacciones ADD CONSTRAINT transacciones_tipo_check
    CHECK (tipo IN ('Recarga','Retiro','TransferenciaEnviada','TransferenciaRecibida'));

-- 2) Un retiro, como una recarga, NO tiene contraparte
ALTER TABLE transacciones DROP CONSTRAINT IF EXISTS ck_contraparte;
ALTER TABLE transacciones ADD CONSTRAINT ck_contraparte CHECK (
    (tipo IN ('Recarga','Retiro')     AND billetera_contraparte_id IS NULL) OR
    (tipo NOT IN ('Recarga','Retiro') AND billetera_contraparte_id IS NOT NULL)
);

-- ---------------------------------------------------- PUNTOS DE FIDELIDAD
-- Un registro por operación que otorga puntos (no un total acumulado):
-- así se sabe por qué se dio cada punto. El total es SUM(puntos).
CREATE TABLE IF NOT EXISTS puntos_fidelidad (
    id              UUID          PRIMARY KEY,
    cliente_id      UUID          NOT NULL REFERENCES clientes(id),
    referencia      UUID          NOT NULL UNIQUE,              -- idempotencia: 1 registro por operación
    tipo_operacion  VARCHAR(30)   NOT NULL,
    monto           NUMERIC(14,2) NOT NULL CHECK (monto > 0),
    puntos          INTEGER       NOT NULL CHECK (puntos > 0),
    fecha           TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_puntos_cliente_fecha
    ON puntos_fidelidad (cliente_id, fecha DESC);

-- Vista de apoyo para clase: total de puntos por cliente
CREATE OR REPLACE VIEW v_puntos AS
SELECT c.nombres || ' ' || c.apellidos AS titular,
       c.email,
       COALESCE(SUM(p.puntos), 0)      AS total_puntos,
       COUNT(p.id)                     AS operaciones
FROM clientes c
LEFT JOIN puntos_fidelidad p ON p.cliente_id = c.id
GROUP BY c.id, c.nombres, c.apellidos, c.email;
