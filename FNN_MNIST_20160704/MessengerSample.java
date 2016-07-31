public class FooViewModel {
	// ...

	public CommandDelegate transitionCommand = new CommandDelegate(
		new Action0() {
			@Override
			public void call() {
				// ...

				EventBus.getDefault().post(new TransitToHogeScreenEvent(
					HogeActivity.class,
					new HogeViewModel(0, ""),
					new Action1<String>() {
						@Override
						public void call(Object response) {
							// ...
						}
					}));
			}
		},
		true);

	public class TransitToHogeScreenEvent {
		private Class<?> activityClass;

		public Class<?> getActivityClass() {
			return activityClass;
		}

		private HogeViewModel viewModel;

		public HogeViewModel getViewModel() {
			return viewModel;
		}

		private Action1<Object> callback;

		public void callCallback(Object response) {
			callback.call(response);
		}

		public ShowHugaAlertDialogEvent(Class<?> activityClass, HogeViewModel viewModel, Action1<Object> callback) {
			this.activityClass = activityClass;
			this.viewModel = viewModel;
			this.callback = callback;
		}
	}
}

public class FooActivity extends Activity {
	// ...

	private FooViewModel fooViewModel;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		// ...

		fooViewModel = FooViewModel.create();

		// ...
	}

	@Override
	protected void onResume() {
		super.onResume();
		EventBus.getDefault().register(this);
	}

	@Override
	protected void onPause() {
		super.onPause();
		EventBus.getDefault().unregister(this);
	}

	public void onEvent(FooViewModel.TransitToHogeScreenEvent event) {
		Intent intent = new Intent(this, event.getActivityClass());
		intent.putExtra("viewModel", event.getViewModel());
		startActivity(intent);
		event.callCallback(null); // コールバック先のオブジェクトが死んでるかもしれない（？）から、画面遷移ではコールバックを用意しないほうがいいかもしれない
	}
}

public class HogeViewModel implements Parcelable {
	public ObservableInt intField;

	public ObservableField<String> stringField;

	public HogeViewModel(int intField, String stringField) {
		this.intField.set(intField);
		this.stringField.set(stringField);
	}

	public HogeViewModel(Parcel in) {
		intField.set(in.readInt());
		stringField.set(in.readString());
	}

	public int describeContents() {
		return 0;
	}

	public void writeToParcel(Parcel out, int flags) {
		out.writeInt(intField.get());
		out.writeString(stringField.get());
	}

	public static final Parcelable.Creator<HogeViewModel> CREATOR = new Parcelable.Creator<HogeViewModel>() {
		public HogeViewModel createFromParcel(Parcel in) {
			return new HogeViewModel(in);
		}

		public ContactParcelable[] newArray(int size) {
			return new HogeViewModel[size];
		}
	}

	// ...

	public CommandDelegate piyoCommand = new CommandDelegate(
		new Action0() {
			@Override
			public void call() {
				// ...

				EventBus.getDefault().post(new ShowHugaAlertDialogEvent(
					"Hello world!",
					new Action1<String>() {
						@Override
						public void call(String response) {
							// ...
						}
					}));
			}
		},
		true);

	// callback 部分についてはインターフェースか抽象クラスにしたほうがいいかもしれない。コールバックの引数に自由度を持たせるためジェネリックにする必要がある
	public class ShowHugaAlertDialogEvent {
		private String message; // String である必要もないし、一つである必要もないはず

		public String getMessage() {
			return message;
		}

		private Action1<String> callback; // String を受け取るコールバックである必要はない

		public void callCallback(String response) {
			callback.call(response);
		}

		public ShowHugaAlertDialogEvent(String message, Action1<String> callback) {
			this.message = message;
			this.callback = callback;
		}
	}
}


public class HogeActivity extends Activity {
	// ...

	private HogeViewModel hogeViewModel;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		// ...

		hogeViewModel = HogeViewModel.create(getIntent().getExtras());

		// ...
	}

	@Override
	protected void onResume() {
		super.onResume();
		EventBus.getDefault().register(this);
	}

	@Override
	protected void onPause() {
		super.onPause();
		EventBus.getDefault().unregister(this);
	}

	public void onEvent(HogeViewModel.ShowHugaAlertDialogEvent event) {
		new AlertDialog.Builder(this)
			.setMessage(event.getMessage())
			.setPositiveButton(android.R.string.yes, new DialogInterface.OnClickListener() {
				// ...

				event.callCallback("positive button clicked");
			})
			.show();
	}
}