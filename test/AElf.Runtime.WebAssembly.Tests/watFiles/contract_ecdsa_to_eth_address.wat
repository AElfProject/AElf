(module
	(import "seal0" "ecdsa_to_eth_address" (func $seal_ecdsa_to_eth_address (param i32 i32) (result i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))

	(func (export "call")
		;; fill the buffer with the eth address.
		(call $seal_ecdsa_to_eth_address (i32.const 0) (i32.const 0))

		;; Return the contents of the buffer
		(call $seal_return
			(i32.const 0)
			(i32.const 0)
			(i32.const 20)
		)

		;; seal_return doesn't return, so this is effectively unreachable.
		(unreachable)
	)
	(func (export "deploy"))
)