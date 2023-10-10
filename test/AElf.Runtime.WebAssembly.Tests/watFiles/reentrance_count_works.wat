(module
	(import "seal0" "reentrance_count" (func $reentrance_count (result i32)))
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
			(call $reentrance_count)
		)
		(call $assert
			(i32.eq (get_local $return_val) (i32.const 12))
		)
	)

	(func (export "deploy"))
)