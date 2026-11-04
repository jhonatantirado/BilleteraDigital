-- =============================================================
--  Billetera Digital — Transferencias por celular + verificación con código
-- =============================================================
-- docker compose lo ejecuta automáticamente SOLO al crear la base por
-- primera vez. Si tu base ya existe, ejecútalo a mano:
--   docker exec -i billetera_postgres psql -U billetera -d billetera_digital < database/05_telefono.sql
-- Es idempotente: puede ejecutarse varias veces sin error.

-- ---------------------------------------------------- 1) NORMALIZAR
-- La API guarda los celulares en formato E.164: '+51987654321'.
-- Se convierten los que ya existen ('987654321', '987 654 321', '51987654321'…).
UPDATE clientes
   SET telefono = '+51' || right(regexp_replace(telefono, '\D', '', 'g'), 9)
 WHERE regexp_replace(telefono, '\D', '', 'g') ~ '^(51)?9\d{8}$'
   AND telefono !~ '^\+519\d{8}$';

-- Formato obligatorio para los datos NUEVOS. NOT VALID: no se revisan (ni se borran)
-- los teléfonos antiguos que no se pudieron normalizar; consúltalos con:
--   SELECT id, email, telefono FROM clientes WHERE telefono !~ '^\+519\d{8}$';
ALTER TABLE clientes DROP CONSTRAINT IF EXISTS ck_clientes_telefono;
ALTER TABLE clientes ADD CONSTRAINT ck_clientes_telefono
    CHECK (telefono ~ '^\+519\d{8}$') NOT VALID;

-- ---------------------------------------------------- 2) VERIFICACIÓN
ALTER TABLE clientes ADD COLUMN IF NOT EXISTS telefono_verificado BOOLEAN NOT NULL DEFAULT false;

-- Único SOLO entre los verificados: si fuera único para todos, cualquiera podría
-- registrarse primero con el número de otra persona y bloquearla ("secuestro").
-- Es la última línea de defensa si dos cuentas verifican el mismo número a la vez.
CREATE UNIQUE INDEX IF NOT EXISTS ux_clientes_telefono_verificado
    ON clientes (telefono) WHERE telefono_verificado;

-- Código pendiente de cada cliente (se guarda el HASH, nunca el código)
CREATE TABLE IF NOT EXISTS verificaciones_telefono (
    cliente_id    UUID        PRIMARY KEY REFERENCES clientes(id) ON DELETE CASCADE,
    codigo_hash   CHAR(64)    NOT NULL,
    expira        TIMESTAMPTZ NOT NULL,
    intentos      INTEGER     NOT NULL DEFAULT 0 CHECK (intentos >= 0),
    ultimo_envio  TIMESTAMPTZ NOT NULL
);

-- ---------------------------------------------------- 3) DATOS DE PRUEBA
-- Ana (+51987654321) y Luis (+51912345678) quedan verificados para la demostración
UPDATE clientes SET telefono_verificado = true
 WHERE id IN ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222')
   AND telefono ~ '^\+519\d{8}$';
