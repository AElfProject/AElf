(module
	;; seal_ecdsa_recover(
	;;    signature_ptr: u32,
	;;    message_hash_ptr: u32,
	;;    output_ptr: u32
	;; ) -> u32
	(import "seal0" "ecdsa_recover" (func $seal_ecdsa_recover (param i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(drop
			(call $seal_ecdsa_recover
				(i32.const 36) ;; Pointer to signature.
				(i32.const 4)  ;; Pointer to message hash.
				(i32.const 36) ;; Pointer for output - public key.
			)
		)
	)
	(func (export "deploy"))
	;; Hash of message.
	(data (i32.const 4)
		"\36\38\36\35\36\63\36\63\36\66\32\30\37\37\36\66"
		"\37\32\36\63\36\34\33\39\33\39\34\38\32\38\30\31"
	)
	;; Signature
	(data (i32.const 36)
	    "\59\EF\1D\3B\2B\85\3F\CA\1E\33\D0\77\65\DE\BA\AF"
	    "\38\A8\14\42\CF\E9\08\22\D4\33\4E\8F\CE\98\89\D8"
	    "\0C\99\A0\BE\18\58\C1\F2\6B\4D\99\98\7E\FF\60\03"
	    "\F3\3B\7C\3F\32\BB\DB\9C\EE\C6\8A\1E\8A\4D\B4\B0"
	    "\00"
	)
)