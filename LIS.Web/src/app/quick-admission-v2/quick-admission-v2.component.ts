import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { QuickAdmissionV2Service, SaveData } from './quick-admission-v2.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-quick-admission-v2',
  standalone: true,
  imports: [
    // Angular modules needed by this component
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './quick-admission-v2.component.html',
  styleUrls: ['./quick-admission-v2.component.scss']
})
export class QuickAdmissionV2Component implements OnInit {
  form!: FormGroup;
  isSaving = false;

  constructor(
    private fb: FormBuilder,
    private service: QuickAdmissionV2Service,
    private route: ActivatedRoute,
    public router: Router
  ) { }

  ngOnInit(): void {
    // build reactive form with nested groups for each domain
    this.form = this.fb.group({
      saveOption: this.fb.group({
        savePatient: [true],
        saveAdmission: [true],
        saveInvoice: [true]
      }),
      patientInfo: this.fb.group({
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        middleName: [''],
        gender: ['M', Validators.required],
        dob: ['', Validators.required],
        phone: [''],
        arabicFullName: [''],
        existingPatientId: [null]
      }),
      admissionInfo: this.fb.group({
        admissionId: [null],
        admissionNumber: [''],
        admissionSite: [3],
        referralPhysicianId: [null, Validators.required],
        attendingPhysicianId: [null],
        departmentId: [null, Validators.required],
        checkInDate: ['', Validators.required],
        checkOutDate: [''],
        type: [3],
        group: [22]
      }),
      invoiceInfo: this.fb.group({
        invoiceHeaderId: [null],
        hospitalAmount: [0],
        physicianAmount: [0],
        gross: [0],
        net: [0]
      }),
      invoiceDetail: this.fb.array([]) 
      // additional groups for delivery etc can be added later
    });

    this.route.params.subscribe(params => {
      if (params['id']) {
        this.loadAdmission(+params['id']);
      }
    });
  }

  private loadAdmission(admissionId: number): void {
    this.service.loadAdmission(admissionId).subscribe({
      next: (data) => {
        this.applySaveData(data);
      },
      error: (err) => {
        console.error('Error loading admission', err);
      }
    });
  }

  private applySaveData(data: SaveData): void {
    if (!data) return;
    if (data.patientInfo) {
      this.form.get('patientInfo')?.patchValue(data.patientInfo);
    }
    if (data.admissionInfo) {
      this.form.get('admissionInfo')?.patchValue(data.admissionInfo);
    }
    if (data.invoiceInfo) {
      this.form.get('invoiceInfo')?.patchValue(data.invoiceInfo);
    }
    if (data.invoiceDetail) {
      const arr = this.form.get('invoiceDetail') as FormArray;
      arr.clear();
      data.invoiceDetail.forEach(d => {
        arr.push(this.fb.group({
          denomination: [d.denomination],
          quantity: [d.quantity],
          unitPrice: [d.unitPrice],
          discount: [d.discount],
          lumpSum: [d.lumpSum]
        }));
      });
    }
    // other groups as needed
  }

  get invoiceDetailArray(): FormArray {
    return this.form.get('invoiceDetail') as FormArray;
  }

  addInvoiceDetail(): void {
    this.invoiceDetailArray.push(this.fb.group({
      denomination: [null],
      quantity: [1],
      unitPrice: [0],
      discount: [0],
      lumpSum: [0]
    }));
  }

  removeInvoiceDetail(index: number): void {
    this.invoiceDetailArray.removeAt(index);
  }

  buildSaveData(): SaveData {
    const formVal = this.form.value;
    // invoiceDetail is maintained as array of values via FormArray.value
    formVal.invoiceDetail = this.invoiceDetailArray.value;
    return formVal as SaveData;
  }

  saveComplete(): void {
    if (this.form.invalid) {
      alert('Please fill out required fields');
      return;
    }
    this.isSaving = true;

    const payload = { saveData: this.buildSaveData(), saveOption: this.form.get('saveOption')?.value };
    this.service.saveComplete(payload).subscribe({
      next: (res) => {
        console.log('SaveComplete response', res);
        alert('Save finished successfully');
        this.isSaving = false;
      },
      error: (err) => {
        console.error('SaveComplete error', err);
        alert('Error saving quick admission');
        this.isSaving = false;
      }
    });
  }
}
