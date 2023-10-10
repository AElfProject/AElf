(module
	(import "seal0" "account_reentrance_count" (func $account_reentrance_count (param i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func $assert (param i32)
		(block $ok
			(br_if $ok
				(get_local 0)
			)
			(unreachable)
		)
	)
	(func (export "call")
		(local $return_val i32)
		(set_local $return_val
			(call $account_reentrance_count (i32.const 0))
		)
		(call $assert
			(i32.eq (get_local $return_val) (i32.const 12))
		)
	)

	(func (export "deploy"))
)