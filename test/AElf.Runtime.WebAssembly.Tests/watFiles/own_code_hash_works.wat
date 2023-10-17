(module
	(import "seal0" "own_code_hash" (func $seal_own_code_hash (param i32 i32)))
	(import "env" "memory" (memory 1 1))

	;; size of our buffer is 32 bytes
	(data (i32.const 32) "\20")
    (data (i32.const 0)
        "\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00"
        "\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00"
    )

	(func $assert (param i32)
		(block $ok
			(br_if $ok
				(get_local 0)
			)
			(unreachable)
		)
	)

	(func (export "call")
		;; fill the buffer with the code hash
		(call $seal_own_code_hash
			(i32.const 0)  ;; output: code_hash_ptr
			(i32.const 32) ;; 32 bytes length of code_hash output
		)

		;; assert size == 32
		(call $assert
			(i32.eq
				(i32.load (i32.const 32))
				(i32.const 32)
			)
		)

		;; assert that the first 8 bytes are "1010101010101010"
		(call $assert
			(i64.eq
				(i64.load (i32.const 0))
				(i64.const 0x1010101010101010)
			)
		)
	)

	(func (export "deploy"))
)