(module
	(import "seal1" "random" (func $seal_random (param i32 i32 i32 i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))

	;; [0,128) is reserved for the result of PRNG.

	;; the subject used for the PRNG. [128,160)
	(data (i32.const 128)
		"\00\01\02\03\04\05\06\07\08\09\0A\0B\0C\0D\0E\0F"
		"\00\01\02\03\04\05\06\07\08\09\0A\0B\0C\0D\0E\0F"
	)

	;; size of our buffer is 128 bytes
	(data (i32.const 160) "\80")

	(func $assert (param i32)
		(block $ok
			(br_if $ok
				(get_local 0)
			)
			(unreachable)
		)
	)

	(func (export "call")
		;; This stores the block random seed in the buffer
		(call $seal_random
			(i32.const 128) ;; Pointer in memory to the start of the subject buffer
			(i32.const 32) ;; The subject buffer's length
			(i32.const 0) ;; Pointer to the output buffer
			(i32.const 160) ;; Pointer to the output buffer length
		)

		;; assert len == 32
		(call $assert
			(i32.eq
				(i32.load (i32.const 160))
				(i32.const 40)
			)
		)

		;; return the random data
		(call $seal_return
			(i32.const 0)
			(i32.const 0)
			(i32.const 40)
		)
	)
	(func (export "deploy"))
)