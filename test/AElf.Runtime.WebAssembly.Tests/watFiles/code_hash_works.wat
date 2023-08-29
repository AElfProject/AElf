(module
	(import "seal0" "code_hash" (func $seal_code_hash (param i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))

	;; size of our buffer is 32 bytes
	(data (i32.const 32) "\20")

	(func $assert (param i32)
		(block $ok
			(br_if $ok
				(get_local 0)
			)
			(unreachable)
		)
	)

	(func (export "call")
		;; fill the buffer with the code hash.
		(call $seal_code_hash
			(i32.const 0) ;; input: address_ptr (before call)
			(i32.const 0) ;; output: code_hash_ptr (after call)
			(i32.const 32) ;; same 32 bytes length for input and output
		)

		;; assert size == 32
		(call $assert
			(i32.eq
				(i32.load (i32.const 32))
				(i32.const 32)
			)
		)

		;; assert that the first 8 bytes are "1111111111111111"
		(call $assert
			(i64.eq
				(i64.load (i32.const 0))
				(i64.const 0x1111111111111111)
			)
		)
		drop
	)

	(func (export "deploy"))
)