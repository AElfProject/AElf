(module
	(import "seal1" "weight_to_fee" (func $seal_weight_to_fee (param i64 i64 i32 i32)))
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
		;; This stores the gas price in the buffer
		(call $seal_weight_to_fee (i64.const 2) (i64.const 1) (i32.const 0) (i32.const 32))

		;; assert len == 8
		(call $assert
			(i32.eq
				(i32.load (i32.const 32))
				(i32.const 8)
			)
		)

		;; assert that contents of the buffer is equal to the i64 value of 2 * 1312 + 103 = 2727.
		(call $assert
			(i64.eq
				(i64.load (i32.const 0))
				(i64.const 1001)
			)
		)
	)
	(func (export "deploy"))
)