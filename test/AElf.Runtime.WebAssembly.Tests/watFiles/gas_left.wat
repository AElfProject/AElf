(module
	(import "seal1" "gas_left" (func $seal_gas_left (param i32 i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))

	;; Make output buffer size 20 bytes
	(data (i32.const 20) "\14")

	(func $assert (param i32)
		(block $ok
			(br_if $ok
				(get_local 0)
			)
			(unreachable)
		)
	)

	(func (export "call")
		;; This stores the weight left to the buffer
		(call $seal_gas_left (i32.const 0) (i32.const 20))

		;; Assert len <= 16 (max encoded Weight len)
		(call $assert
			(i32.le_u
				(i32.load (i32.const 20))
				(i32.const 16)
			)
		)

		;; Return weight left and its encoded value len
		(call $seal_return (i32.const 0) (i32.const 0) (i32.load (i32.const 20)))

		(unreachable)
	)
	(func (export "deploy"))
)